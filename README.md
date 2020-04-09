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

v1.2.0.2

- Minor cleanup, version from assembly, dependency update, XML documentation, Postman collection

## Help and Feedback

First things first - do you need help or have feedback?  Please file an issue here. 

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
  - WriteTags
  - WriteVersioning (no MFA delete support)
  - Delete
  - DeleteTags
  - Exists
  - Read (list objects v2)
  - ReadAcl
  - ReadVersions
  - ReadTags

- Object APIs
  - Write
  - WriteAcl
  - WriteTags
  - Delete
  - DeleteMultiple
  - DeleteTags
  - Exists
  - Read
  - ReadAcl
  - ReadRange
  - ReadTags

## API Support

There are several minor differences between how S3 and less3 handle certain aspects of API requests.  However, these should be inconsequential from the perspective of the developer (for instance, version IDs are numbers internally within less3 rather than strings).  

Should you find any incompatibilities or behavioral issues with the APIs listed above that are considered 'supported', please file an issue here along with details on the expected behavior.  I've tried to mimic the behavior of S3 while building out the API logic.  A link to the supporting documentation will also be helpful to aid me in righting the wrong :)

## Bucket in Hostname vs URL

Less3 supports cases where having the bucket name as:
- Part of the URL (```http://hostname.com/bucket/key```)
- Part of the hostname (```http://bucket.hostname.com/key```)  

To support the latter cases where the bucket should be part of the hostname, set the ```Server.BaseDomain``` to an appropriate value.  For instance, if the bucket name is ```bucket``` and you would like it to be accessible via ```bucket.hostname.com```, set ```Server.BaseDomain``` to ```.hostname.com``` within ```System.json```.

If this parameter is not set, Less3 will always assume the bucket name is part of the URL.  If this parameter is set, and the base domain cannot be found within the incoming request hostname, Less3 will assume the bucket name is part of the URL.

## Administrative APIs

Please refer to the 'wiki' for helpful notes including how to use the administrative APIs.

## Open Source Packages 

Less3 is built using a series of open-source packages, including:

- AWS SDK - https://github.com/aws/aws-sdk-net
- S3 Server Interface - https://github.com/jchristn/s3serverinterface
- Watson Webserver - https://github.com/jchristn/WatsonWebserver

## Version History

Refer to CHANGELOG.md for details.
