# Use the official .NET SDK image for building
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy the solution file and project files
COPY Valora.slnx .
COPY Valora.Api/Valora.Api.csproj Valora.Api/
COPY Valora.Application/Valora.Application.csproj Valora.Application/
COPY Valora.Domain/Valora.Domain.csproj Valora.Domain/
COPY Valora.Infrastructure/Valora.Infrastructure.csproj Valora.Infrastructure/

# Restore dependencies
RUN dotnet restore Valora.Api/Valora.Api.csproj

# Copy the rest of the source code
COPY . .

# Build and publish the application
WORKDIR /src/Valora.Api
RUN dotnet publish -c Release -o /app/publish /p:UseAppHost=false

# Use the official ASP.NET Core runtime image
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
RUN apt-get update && apt-get install -y libgssapi-krb5-2 && rm -rf /var/lib/apt/lists/*
WORKDIR /app
EXPOSE 80
EXPOSE 443

COPY --from=build /app/publish .

ENTRYPOINT ["dotnet", "Valora.Api.dll"]
