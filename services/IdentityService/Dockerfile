FROM mcr.microsoft.com/dotnet/sdk:8.0
WORKDIR /app
COPY services/IdentityService/ ./IdentityService/
COPY services/Common/ ./Common/
WORKDIR /app/IdentityService
RUN dotnet publish -c Release -o ./publish
WORKDIR /app/IdentityService/publish
ENTRYPOINT ["dotnet", "IdentityService.dll"]