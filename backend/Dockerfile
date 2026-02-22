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

# Use the official ASP.NET Core runtime image (Alpine based for size optimization)
FROM mcr.microsoft.com/dotnet/aspnet:10.0-alpine AS final
# Install dependencies for SQL Server (GSSAPI) and Globalization (ICU)
RUN apk add --no-cache icu-libs krb5-libs libintl
# Ensure globalization uses ICU instead of invariant mode (needed for many .NET apps)
ENV DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=false

WORKDIR /app
EXPOSE 80
EXPOSE 443

COPY --from=build /app/publish .

ENTRYPOINT ["dotnet", "Valora.Api.dll"]
