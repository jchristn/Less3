FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build-env
WORKDIR /app

# Copy everything from source directory into /App
COPY . ./
EXPOSE 8000
ENTRYPOINT ["dotnet", "Less3.dll"]
