FROM mcr.microsoft.com/dotnet/core/sdk:3.1-alpine as build

COPY src/. .
RUN dotnet restore
RUN dotnet publish -c Release -o out

FROM mcr.microsoft.com/dotnet/core/runtime:3.1-alpine

WORKDIR /app
COPY --from=build /out .
ENTRYPOINT ["dotnet", "DynamicDns.dll"] 