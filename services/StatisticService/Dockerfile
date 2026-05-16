FROM mcr.microsoft.com/dotnet/sdk:8.0
WORKDIR /app
COPY services/StatisticService/ ./StatisticService/
COPY services/Common/ ./Common/
WORKDIR /app/StatisticService
RUN dotnet publish -c Release -o ./publish
WORKDIR /app/StatisticService/publish
ENTRYPOINT ["dotnet", "StatisticsService.dll"]