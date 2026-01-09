#
#
# Run the docker build command from src, not from the project directory
#
#

#
#
# Build stage
#
#
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy source
COPY . .

# Restore dependencies
RUN dotnet restore "Less3/Less3.csproj"

# Build the application
WORKDIR /src/Less3
RUN dotnet build "Less3.csproj" -c Release -o /app/build /p:GeneratePackageOnBuild=false


#
#
# Publish stage
#
#
FROM build AS publish
RUN dotnet publish "Less3.csproj" -c Release -o /app/publish /p:UseAppHost=false /p:GeneratePackageOnBuild=false /p:ErrorOnDuplicatePublishOutputFiles=false


#
#
# Runtime stage
#
#
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS final
RUN apt-get update && apt-get install -y \
    # Networking tools
    iputils-ping \
    traceroute \
    net-tools \
    curl \
    wget \
    dnsutils \
    iproute2 \
    netcat-openbsd \
    telnet \
    tcpdump \
    # Editors and utilities
    vim \
    less \
    file \
    jq \
    # Process tools
    procps \
    htop \
    && rm -rf /var/lib/apt/lists/*
WORKDIR /app
COPY --from=publish /app/publish .
EXPOSE 8000
ENTRYPOINT ["dotnet", "Less3.dll"]
