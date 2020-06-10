# Running Less3 in Docker
 
Getting an ```HttpListener``` application (such as Less3 or any application using Watson Webserver, HTTP.sys, etc) up and running in Docker can be rather tricky given how 1) Docker acts as a network proxy and 2) HttpListener isn't friendly to ```HOST``` header mismatches.  Thus, it is **critical** that you run your containers using ```--user ContainerAdministrator``` (Windows) or ```--user root``` (Linux or Mac) to bypass the ```HttpListener``` restrictions.  There are likely ways around this, but I have been unable to find one.  

## Before you Begin

As a persistent storage platform, data stored within the container, will be lost once the container terminates.  Similarly, any metadata stored in a database would be lost if the database resides within the container.  Likewise, any object data stored within the filesystem of the container would also be lost should the container terminate.

As such, it is important that you properly deploy Less3 when using containers.  Use the following best practices.

### Case Sensitivity!

Certain operating systems may be case-sensitive when it comes to file names.  Be specific in the case you use within the ```Dockerfile```.

### Copy in Node Configuration

The ```system.json``` file which defines the configuration for your node should be copied in as part of your ```Dockerfile```.  Do not allow it to be built dynamically.

Also, if you will be detaching (i.e. the ```-d``` flag in ```docker run```) be sure to set ```system.json``` ```EnableConsole``` to false and ```Logging.ConsoleLogging``` to false.

Be sure to set your ```system.json``` ```Server.DnsHostname``` to ```*```.

### Use an External Database

Less3 relies on a database for storing object (and other) metadata.  While Less3 is capable of using Sqlite, Sqlite databases are stored on the filesystem within the container which will be lost once the container terminates.  Use an external database such as SQL Server, MySQL, or PostgreSQL, or, use Sqlite with a file stored on external storage.  Modify the ```system.json``` ```Database``` section accordingly. 

Valid values for ```Type``` are: ```Mysql```, ```SqlServer```, ```Postgresql```, and ```Sqlite```
```
  "Database": {
    "Type": "Mysql",  
    "Hostname": "[database server hostname]",
    "Port": 3306,
    "DatabaseName": "less3",
    "InstanceName": null,
    "Username": "root",
    "Password": "[password]"
  }
```

### Use External Storage

Less3 stores object data on the filesystem.  While the filesystem location could be local, this is not recommended for containerized deployments because the container filesystem is destroyed when the container is terminated.  As such, it is recommended that Less3 rely on an NFS export or CIFS file share (or some other underlying shared storage) when deployed in a container.

Modify the ```system.json``` ```Storage``` section accordingly.  If necessary, modify the ```Dockerfile``` to issue the appropriate commands to establish the connection to the external or shared storage as part of the build process.  The ```TempDirectory``` property can remain local to the container as it is only used temporarily as objects are written.
```
  "Storage": {
    "DiskDirectory": "[NFS, CIFS, or other shared or external storage mount]"
  }
```

Note: Less3 has a class called ```StorageDriver``` that allows you to implement your own storage interface.  This can be used to interface Less3 with external storage services such as those provided by cloud providers.

## Steps to Run Less3 in Docker

1) View and modify the ```Dockerfile``` as appropriate for your application.

2) Execute the Docker build process:
```
$ docker build -t less3 -f Dockerfile .
```

3) Verify the image exists:
```
$ docker images
REPOSITORY                              TAG                 IMAGE ID            CREATED             SIZE
less3                                   latest              047e29f37f9c        2 seconds ago       328MB
mcr.microsoft.com/dotnet/core/sdk       3.1                 abbb476b7b81        11 days ago         737MB
mcr.microsoft.com/dotnet/core/runtime   3.1                 4b555235dfc0        11 days ago         327MB
```
 
4) Execute the container:
```
Windows
$ docker run --user ContainerAdministrator -d -p 8000:8000 less3 

Linux or Mac 
$ docker run --user root -d -p 8000:8000 less3
```

5) Connect to Less3 in your browser: 
```
http://localhost:8000
```

6) Get the container name:
```
$ docker ps
CONTAINER ID        IMAGE               COMMAND                  CREATED              STATUS              PORTS                    NAMES
3627b4e812fd        less3               "dotnet Less3.dll"       About a minute ago   Up About a minute   0.0.0.0:8000->8000/tcp   silly_khayyam
```

7) Kill a running container:
```
$ docker kill [CONTAINER ID]
```

8) Delete a container image:
```
$ docker rmi [IMAGE ID] -f
```

## Example system.json File

Notice in the ```system.json``` example provided below that:

- ```EnableConsole``` and ```Logging.ConsoleLogging``` are false, so it is safe to detach using ```-d``` in ```docker run```
- An external ```Mysql``` database is being used, so object metadata will persist even when the container is terminated
- External storage is used for object data, so object data will persist even when the container is terminated

```
{
  "EnableConsole": false,
  "Database": {
    "Type": "Mysql",  
    "Hostname": "[database server hostname]",
    "Port": 3306,
    "DatabaseName": "less3",
    "InstanceName": null,
    "Username": "root",
    "Password": "[password]"
  },
  "Server": {
    "DnsHostname": "localhost",
    "ListenerPort": 8000,
    "Ssl": false,
    "HeaderApiKey": "x-api-key",
    "AdminApiKey": "less3admin",
    "RegionString": "us-west-1"
  },
  "Storage": {
    "TempDirectory": "./Temp/",
    "StorageType": "Disk",
    "DiskDirectory": "/mnt/less3disk/"
  },
  "Logging": {
    "SyslogServerIp": "127.0.0.1",
    "SyslogServerPort": 514,
    "Header": "less3",
    "MinimumLevel": 1,
    "LogHttpRequests": false,
    "ConsoleLogging": false,
    "DiskLogging": false
  },
  "Debug": {
    "DatabaseQueries": false,
    "DatabaseResults": false,
    "Authentication": false,
    "S3Requests": false
  }
}
```