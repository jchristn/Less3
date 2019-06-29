![alt tag](https://github.com/kvpbase/less3/blob/master/assets/logo.png)

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

The following APIs are supported with Less3:
- insert here

## Unsupported APIs

The following APIs are not supported with Less3:
- insert here

## Version History

Notes from previous versions (starting with v1.0.x) will be moved here.
