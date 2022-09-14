# Testing with AWS CLI

Use the following commands to test a fresh installation of Less3 with the AWS CLI.  

Important: if you encounter any issues, re-submit the AWS CLI command using the ```--debug``` option, and please provide that output when filing an issue.

## Install Less3
```
> less3


   _           ____
  | |___ _____|__ /
  | / -_|_-<_-<|_ \
  |_\___/__/__/___/




<3 :: Less3 :: S3-Compatible Object Storage

Thank you for using Less3!  We're putting together a basic system configuration
so you can be up and running quickly.  You'll want to modify the system.json
file after to ensure a more secure operating environment.


Less3 requires access to a database and supports Sqlite, Microsoft SQL Server,
MySQL, and PostgreSQL.  Please provide access details for your database.  The
user account supplied must have the ability to CREATE and DROP tables along
with issue queries containing SELECT, INSERT, UPDATE, and DELETE.  Setup will
attempt to create tables on your behalf if they dont exist.

Database type [sqlite|sqlserver|mysql|postgresql]: [sqlite]
Filename: [./less3.db]

IMPORTANT: Using Sqlite in production is not recommended if deploying within a
containerized environment and the database file is stored within the container.
Store the database file in external storage to ensure persistence.


All finished!

If you ever want to return to this setup wizard, just re-run the application
from the terminal with the 'setup' argument.

We created a bucket containing a few sample files for you so that you can see
your node in action.  Access these files in the 'default' bucket using the
AWS SDK or your favorite S3 browser tool.

  http://localhost:8000/default/hello.html
  http://localhost:8000/default/hello.txt
  http://localhost:8000/default/hello.json

  Access key  : default
  Secret key  : default
  Bucket name : default (public read enabled!)
  S3 endpoint : http://localhost:8000

IMPORTANT: be sure to supply a hostname in the system.json Server.DnsHostname
field if you wish to allow access from other machines.  Your node is currently
only accessible via localhost.  Do not use an IP address for this value.




  ,d88b.d88b,    _           ____
  88888888888   | |___ _____|__ /
  `Y8888888Y'   | / -_|_-<_-<|_ \
    `Y888Y'     |_\___/__/__/___/
      `Y'



Less3 | S3-Compatible Object Storage | v2.1.0.0

WARNING: Less3 started on 'localhost'
Less3 can only service requests from the local machine.  If you wish to serve
external requests, edit the system.json file and specify a DNS-resolvable
hostname in the Server.DnsHostname field.

| Initializing logging
| Initializing database
| Initializing configuration manager
| Initializing bucket manager
| Initializing authentication manager
| Initializing API handler
| Initializing admin API handler
| Initializing console manager
| Initializing S3 server interface
| http://localhost:8000
| Initializing S3 server APIs
| No base domain specified
  | Requests must use path-style hosted URLs, i.e. [hostname]/[bucket]/[key]

Command (? for help) >
```

## Set Access Material
```
$ aws configure
AWS Access Key ID [****************ault]: default
AWS Secret Access Key [****************ault]: default
Default region name [us-west-1]: us-west-1
Default output format [None]:
```

## List Buckets
```
$ aws --endpoint-url http://localhost:8000 s3 ls s3://
2022-09-14 21:43:27 default
```

## List Default Bucket
```
$ aws --endpoint-url http://localhost:8000 s3 ls s3://default
2022-09-14 21:43:27       1193 hello.html
2022-09-14 21:43:27        217 hello.txt
2022-09-14 21:43:27        152 hello.json
```

## Download an Object
```
$ aws --endpoint-url http://localhost:8000 s3 cp s3://default/hello.json ./hello.json
download: s3://default/hello.json to .\hello.json
```

## Upload an Object

Versioning is disabled by default on buckets.  Re-uploading the object that was just downloaded would fail.  Rename the object first.

```
$ aws --endpoint-url http://localhost:8000 s3 cp ./hello.foo s3://default/hello.foo
upload: .\hello.foo to s3://default/hello.foo
```

## Delete an Object
```
$ aws --endpoint-url http://localhost:8000 s3 rm s3://default/hello.foo
delete: s3://default/hello.foo
```

## Create a Bucket
```
$ aws --endpoint-url http://localhost:8000 s3 mb s3://bucket2
make_bucket: bucket2
```

## Remove a Bucket
```
$ aws --endpoint-url http://localhost:8000 s3 rb s3://bucket2
remove_bucket: bucket2
```
