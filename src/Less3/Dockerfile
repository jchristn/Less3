# Use SDK
FROM mcr.microsoft.com/dotnet/core/sdk:3.1 AS build

# Set the local working directory, copy in the project file, and perform a restore
WORKDIR /app
COPY *.csproj .
RUN dotnet restore

# Copy everything else and build
COPY . ./
RUN dotnet build   -f netcoreapp3.1 -c Release
RUN dotnet publish -f netcoreapp3.1 -c Release -o out

# Build runtime image
FROM mcr.microsoft.com/dotnet/core/runtime:3.1
WORKDIR /app
EXPOSE 8000/tcp
COPY --from=build /app/out .

#
# Copy in the system.json file containing the appropriate external
# database and storage settings.  Refer to Docker.md for details.
#
COPY ./system.json .
COPY ./less3.db .
RUN mkdir Logs
RUN mkdir Storage
RUN mkdir Temp
#

#
# Since certain operating systems are case sensitive when it comes
# to files in the file system, you may want to uncomment the
# following line so you can see the exact case used.
#
# This is particularly important for copying in system.json, any
# Sqlite database files, and setting the ENTRYPOINT DLL.
#
# RUN ls -la
#

# Set the entrypoint for the container
ENTRYPOINT ["dotnet", "Less3.dll"]