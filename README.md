![alt tag](https://github.com/jchristn/less3/blob/master/assets/logo.png)

# Less3 :: S3-Compatible Object Storage

Less3 is an S3-compatible object storage platform that you can run anywhere. 

![alt tag](https://github.com/jchristn/less3/blob/master/assets/diagram.png)

## Use Cases

Core use cases for Less3:

- Local object storage - S3-compliant storage on your laptop, virtual machine, container, or bare metal
- Development and test - local devtest against S3 compatible storage
- Remote storage - deploy S3 compliant storage in environments where you must control data placement

## New in This Version

v1.0.x
- Initial release; please see supported APIs below.

## Help and Feedback

First things first - do you need help or have feedback?  Contact me at joel dot christner at gmail dot com or file an issue here. 

## Initial Setup

The binaries for Less3 can be created by compiling from source.  Executing the binary will create a series of JSON files containing the configuration for your node, including:

- System.json - system configuration
- Buckets.json - list of buckets exposed by less3
- Users.json - list of users
- Credentials.json - list of access keys

The ```Server.DnsHostname``` MUST be set to a hostname.  You cannot use IP addresses (parsing will fail).  Incoming HTTP requests must have a HOST header value that matches the value in ```Server.DnsHostname```.  If it does not match, you will receive a ```400/Bad Request```.

If you use ```*``` or ```+``` for the ```Server.DnsHostname```, Less3 must be executed using administrative privileges.

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
  - WriteTags
  - WriteVersioning (no MFA delete support)
  - Delete
  - DeleteTags
  - Exists
  - Read (list objects v2)
  - ReadVersions
  - ReadTags

- Object APIs
  - Write
  - WriteTags
  - Delete
  - DeleteMultiple
  - DeleteTags
  - Exists
  - Read
  - ReadRange
  - ReadTags

## API Support

There are several minor differences between how S3 and less3 handle certain aspects of API requests.  However, these should be inconsequential from the perspective of the developer (for instance, version IDs are numbers internally within less3 rather than strings).  

Should you find any incompatibilities or behavioral issues with the APIs listed above that are considered 'supported', please file an issue here along with details on the expected behavior.  I've tried to mimic the behavior of S3 while building out the API logic.  A link to the supporting documentation will also be helpful to aid me in righting the wrong :)
 
## Authentication and Authorization

As of release v1.0.x, only primitive authentication and authorization supported, i.e. you cannot specify specific privileges to assign to access keys.
  
## Open Source Packages 

Less3 is built using a series of open-source packages, including:

- AWS SDK - https://github.com/aws/aws-sdk-net
- S3 Server Interface - https://github.com/jchristn/s3serverinterface
- Watson Webserver - https://github.com/jchristn/WatsonWebserver

## Version History

Notes from previous versions (starting with v1.0.x) will be moved here.
