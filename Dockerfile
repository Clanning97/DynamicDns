FROM mcr.microsoft.com/dotnet/sdk:5.0-alpine as build

COPY src/. .
RUN dotnet restore
RUN dotnet publish -c Release -o out

FROM mcr.microsoft.com/dotnet/runtime:5.0-alpine

WORKDIR /app
COPY --from=build /out .
ENTRYPOINT ["dotnet", "DynamicDns.dll"] 