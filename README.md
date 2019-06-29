![alt tag](https://github.com/jchristn/less3/blob/master/assets/logo.png)

# Less3 :: S3-Compatible Object Storage

Less3 is an S3-compatible object storage platform that you can run anywhere. 

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

The binaries for Less3 can be created by compiling from source.  Executing the binary will create a ```system.json``` file containing the configuration for your node.

In Linux and Mac environments, the listener hostname ```Server.DnsHostname``` MUST be a hostname or IP address.  Incoming requests must have a HOST header matching this exact value.  If it does not match, you will receive a ```400/Bad Request```.

In Windows environments, you can use a DNS hostname or IP address.  If you use ```*```, ```+```, or ```0.0.0.0``` to listen on all IP addresses and hostnames, Less3 must be executed using administrative privileges.
 
## S3 Client Compatibility

Less3 was designed to be consumed using either the AWS SDK or direct RESTful integration in accordance with their official documentation (https://docs.aws.amazon.com/AmazonS3/latest/API/Welcome.html).  Should you encounter a discrepancy between how Less3 operates and how AWS S3 operates, please file an issue.
 
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

## Compatibility and Testing

I tested Less3 using the AWS SDK for C#, a live account on S3, CloudBerry Explorer for S3, and S3 Browser.  If you have or recommend other tools, please file an issue here and let me know!

## Authentication and Authorization

As of release v1.0.x, only primitive authentication and authorization supported, i.e. you cannot specify specific privileges to assign to access keys.

## Version History

Notes from previous versions (starting with v1.0.x) will be moved here.
