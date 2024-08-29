![alt tag](https://github.com/jchristn/less3/blob/master/assets/logo.png)

# Less3 :: S3-Compatible Object Storage

Less3 is an S3-compatible object storage platform that you can run anywhere. 

![alt tag](https://github.com/jchristn/less3/blob/master/assets/diagram.png)

## Use Cases

Core use cases for Less3:

- Local object storage - S3-compatible storage on your laptop, virtual machine, container, or bare metal
- Private cloud object storage - use your existing private cloud hardware to create an S3-compatible storage pool
- Development and test - local devtest against S3-compatible storage
- Remote storage - deploy S3-compatible storage in environments where you must control data placement

## New in This Version

v2.1.x

- Dependency update and changes to improve compatibility with AWS CLI
- Testing with key AWS CLI capabilities, see AWSCLI.md

## Help and Feedback

First things first - do you need help or have feedback?  Please file an issue here. 

## Special Thanks

Thanks to @iain-cyborn for helping make the platform better!

## Initial Setup

The binaries for Less3 can be created by compiling from source.  Executing the binary will create a system configuration in the ```system.json``` file along with the configuration database ```less3.db```. 

The ```Server.DnsHostname``` MUST be set to a hostname.  You cannot use IP addresses (parsing will fail).  Incoming HTTP requests must have a HOST header value that matches the value in ```Server.DnsHostname```.  If it does not match, you will receive a ```400/Bad Request```.

If you use ```*```, ```+```, or ```0.0.0.0``` for the ```Server.DnsHostname```, Less3 must be executed using administrative privileges (this is required by the underlying operating system).

To get started, clone Less3, build, publish, and run!

```
$ git clone https://github.com/jchristn/less3
$ cd less3
$ dotnet build -f netcoreapp2.2
$ dotnet publish -f netcoreapp2.2
$ cd less3/bin/debug/netcoreapp2.2/publish
$ dotnet less3.dll
```

## S3 Client Compatibility

Less3 was designed to be consumed using either the AWS SDK or direct RESTful integration in accordance with Amazon's official documentation (https://docs.aws.amazon.com/AmazonS3/latest/API/Welcome.html).  Should you encounter a discrepancy between how Less3 operates and how AWS S3 operates, please file an issue.
 
I tested Less3 using the AWS SDK for C#, a live account on S3, CloudBerry Explorer for S3 (see https://www.cloudberrylab.com/explorer/windows/amazon-s3.aspx), and S3 Browser (see http://s3browser.com/).  If you have or recommend other tools, please file an issue here and let me know!

## Supported APIs

Please refer to the compatibility matrix found in 'assets' for a full list of supported APIs and caveats.

The following APIs are supported with Less3:

- Service APIs
  - ListBuckets

- Bucket APIs
  - Write
  - WriteAcl
  - WriteTagging
  - WriteVersioning (no MFA delete support)
  - Delete
  - DeleteTagging
  - Exists
  - Read (list objects v2)
  - ReadAcl
  - ReadVersions
  - ReadTagging

- Object APIs
  - Write
  - WriteAcl
  - WriteTagging
  - Delete
  - DeleteMultiple
  - DeleteTagging
  - Exists
  - Read
  - ReadAcl
  - ReadRange
  - ReadTagging

## API Support

There are several minor differences between how S3 and less3 handle certain aspects of API requests.  However, these should be inconsequential from the perspective of the developer (for instance, version IDs are numbers internally within less3 rather than strings).  

Should you find any incompatibilities or behavioral issues with the APIs listed above that are considered 'supported', please file an issue here along with details on the expected behavior.  I've tried to mimic the behavior of S3 while building out the API logic.  A link to the supporting documentation will also be helpful to aid me in righting the wrong :)

## Bucket in Hostname vs URL

Less3 supports cases where having the bucket name as:
- **Path style URLs** - the bucket name is part of the URL (```http://[hostname]/[bucket]/[key]```)
- **Virtual hosted URLs** - the bucket name is part of the hostname (```http://[bucket].[hostname]/[key]```)  

To use **path style URLs** do **not** set ```Server.BaseDomain```.  This is the default configuration.

To use **virtual hosted URLs**, you must:

- Set ```Server.BaseDomain``` - if your hostname is ```localhost```, set this value to ```.localhost``` (prepend with a period)
- Set ```Server.DnsHostname``` to ```*```
- Run Less3 as administrator
- Ensure your hosted hostnames (i.e. ```[bucket].[hostname]```) are resolvable through DNS to your machine

## Administrative APIs

Please refer to the 'wiki' for helpful notes including how to use the administrative APIs.

## Open Source Packages 

Less3 is built using a series of open-source packages, including:

- AWS SDK - https://github.com/aws/aws-sdk-net
- S3 Server - https://github.com/jchristn/s3server
- Watson Webserver - https://github.com/jchristn/WatsonWebserver
- WatsonORM - https://github.com/jchristn/watsonorm

## Deployment in Docker

Less3 is available in [DockerHub](https://hub.docker.com/r/jchristn/less3).  Refer to the `Docker` directory for the `compose.yaml`, `system.json`, and `less3.db` files that you will need.  By default, the node will listen on TCP port `8000` and you can use the access key `default` and secret key `default`.  Be sure to connect without SSL, and use path-style URLs.

## Version History

Refer to CHANGELOG.md for details.
