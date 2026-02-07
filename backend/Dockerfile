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
WORKDIR /app
EXPOSE 80
EXPOSE 443

# Install Playwright dependencies (system libraries for Chromium)
# Note: Using t64 suffixes for some packages to match Ubuntu Noble/Debian Trixie
RUN apt-get update && apt-get install -y --no-install-recommends \
    libnss3 \
    libnspr4 \
    libatk1.0-0t64 \
    libatk-bridge2.0-0t64 \
    libcups2 \
    libdrm2 \
    libxkbcommon0 \
    libxcomposite1 \
    libxdamage1 \
    libxext6 \
    libxfixes3 \
    libxrandr2 \
    libgbm1 \
    libpango-1.0-0 \
    libcairo2 \
    libasound2t64 \
    && rm -rf /var/lib/apt/lists/*

COPY --from=build /app/publish .

# Install Chromium browser for Playwright
# We use 'dotnet exec' with the app's runtime config to run the Playwright installer
RUN dotnet exec --runtimeconfig Valora.Api.runtimeconfig.json Microsoft.Playwright.dll install chromium

ENTRYPOINT ["dotnet", "Valora.Api.dll"]
