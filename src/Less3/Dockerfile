#
#
# Run the docker build command from src, not from the project directory.
# Requires --build-context s3server=<path-to-s3server-6.0-repo>
#
# Example:
#   docker buildx build -f Less3\Dockerfile --build-context s3server=..\..\s3server-6.0 ...
#
#

#
#
# Build stage
#
#
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build

# Workaround for QEMU emulation issues with .NET on ARM64
ENV DOTNET_EnableWriteXorExecute=0

WORKDIR /build/less3/src

# Copy Less3 source from primary context
COPY . .

# Copy S3Server source from named build context (s3server-6.0 repo).
# The directory structure must preserve the relative project reference path
# in Less3.csproj: ..\..\..\s3server-6.0\src\S3Server\S3Server.csproj
COPY --from=s3server src/S3Server/ /build/s3server-6.0/src/S3Server/
COPY --from=s3server LICENSE.md /build/s3server-6.0/LICENSE.md
COPY --from=s3server README.md /build/s3server-6.0/README.md

# Restore dependencies
RUN dotnet restore "Less3/Less3.csproj"

# Build the application
WORKDIR /build/less3/src/Less3
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
