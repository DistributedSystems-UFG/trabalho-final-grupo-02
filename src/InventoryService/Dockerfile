FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy csproj and restore
COPY ["InventoryService.csproj", "."]
RUN dotnet restore "InventoryService.csproj"

# Copy everything else and build
COPY . .
RUN dotnet publish "InventoryService.csproj" -c Release -o /app/publish

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .

# Internal Docker network, no TLS
ENV ASPNETCORE_URLS=http://+:5001
EXPOSE 5001

ENTRYPOINT ["dotnet", "InventoryService.dll"]