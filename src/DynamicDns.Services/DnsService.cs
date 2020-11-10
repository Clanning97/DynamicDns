using Amazon;
using Amazon.Route53;
using Amazon.Route53.Model;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;

namespace DynamicDns.Services
{
    public interface IDnsService
    {
        Task CreateOrUpdate(string ip, string domain);
    }

    public class DnsService : IDnsService
    {
        private readonly AmazonRoute53Client _client;
        private readonly ILogger<IDnsService> _logger;

        public DnsService(IConfiguration configuration, ILogger<IDnsService> logger)
        {
            var awsAccessKeyId = configuration["AWS_ACCESS_KEY_ID"];
            var awsSecretAccessKey = configuration["AWS_SECRET_ACCESS_KEY"];
            var awsRegion = configuration["AWS_REGION"];

            _client = new AmazonRoute53Client(awsAccessKeyId, awsSecretAccessKey, RegionEndpoint.GetBySystemName(awsRegion));
            _logger = logger;
        }

        public async Task CreateOrUpdate(string ip, string domain)
        {
            HostedZone domainZone = null;

            try
            {
                var hostedZones = (await _client.ListHostedZonesAsync()).HostedZones;

                foreach (var zone in hostedZones)
                {
                    if (zone.Name.Contains(domain)) domainZone = zone;
                }
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Could not fetch hosted zones");
                throw;
            }


            if (domainZone == null)
            {
                throw new Exception($"Could not find hosted zone for {domain}");
            }

            List<ResourceRecordSet> recordSet;
            try
            {
                var resourceRecordSetsRequest = new ListResourceRecordSetsRequest
                {
                    HostedZoneId = domainZone.Id
                };
                recordSet = (await _client.ListResourceRecordSetsAsync(resourceRecordSetsRequest)).ResourceRecordSets;
            }
            catch (Exception e)
            {
                _logger.LogError(e, $"Could not retrieve ResourceRecordSets for {domainZone.Name}");
                throw;
            }

            var found = false;
            for (int i = 0; i < recordSet.Count && !found; i++)
            {
                var record = recordSet[i];

                if (record.Name == domain + '.' && record.Type == RRType.A)
                {
                    found = true;

                    foreach (var resourceRecord in record.ResourceRecords)
                    {
                        if (resourceRecord.Value != ip)
                        {
                            await UpdateRecord(domainZone.Id, domain, ip);
                            return;
                        }
                    }
                }
            }
            if (found == false)
            {
                await CreateRecord(domainZone.Id, domain, ip);
            }
        }

        private async Task UpdateRecord(string zoneId, string fqdn, string ip)
        {
            var request = new ChangeResourceRecordSetsRequest
            {
                HostedZoneId = zoneId,
                ChangeBatch = new ChangeBatch
                {
                    Changes = new List<Change>
                    {
                        new Change
                        {
                            Action = ChangeAction.UPSERT,
                            ResourceRecordSet = new ResourceRecordSet
                            {
                                Name = fqdn,
                                Type = RRType.A,
                                TTL = 300,
                                ResourceRecords = new List<ResourceRecord>
                                {
                                    new ResourceRecord(ip)
                                }
                            }
                        }
                    }
                }
            };

            try
            {
                var response = await _client.ChangeResourceRecordSetsAsync(request);

                if (response.HttpStatusCode != HttpStatusCode.OK) throw new Exception("Something happened when trying to update record");
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Could not send change when updating record");
                throw;
            }
        }

        private async Task CreateRecord(string zoneId, string fqdn, string ip)
        {
            var request = new ChangeResourceRecordSetsRequest
            {
                HostedZoneId = zoneId,
                ChangeBatch = new ChangeBatch
                {
                    Changes = new List<Change>
                    {
                        new Change
                        {
                            Action = ChangeAction.CREATE,
                            ResourceRecordSet = new ResourceRecordSet
                            {
                                Name = fqdn,
                                Type = RRType.A,
                                TTL = 300,
                                ResourceRecords = new List<ResourceRecord>
                                {
                                    new ResourceRecord(ip)
                                }
                            }
                        }
                    }
                }
            };


            try
            {
                var response = await _client.ChangeResourceRecordSetsAsync(request);

                if (response.HttpStatusCode != HttpStatusCode.OK) throw new Exception("Something happened when trying to create record");
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Could not send change when creating record");
                throw;
            }
        }
    }
}