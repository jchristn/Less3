@echo off
aws configure

echo Listing buckets...
aws --endpoint-url http://localhost:8000 s3 ls s3://

echo Checking if bucket default exists...
aws s3api head-bucket --endpoint http://localhost:8000 --bucket default

echo Listing bucket default...
aws --endpoint-url http://localhost:8000 s3 ls s3://default

echo Downloading hello.json...
aws --endpoint-url http://localhost:8000 s3 cp s3://default/hello.json ./hello.json

echo Copying default.json to hello.foo...
copy /Y hello.json hello.foo

echo Uploading hello.foo...
aws --endpoint-url http://localhost:8000 s3 cp ./hello.foo s3://default/hello.foo

echo Checking if object hello.foo exists...
aws s3api head-object --endpoint http://localhost:8000 --bucket default --key hello.foo

echo Deleting hello.foo...
aws --endpoint-url http://localhost:8000 s3 rm s3://default/hello.foo

echo Creating bucket bucket2...
aws --endpoint-url http://localhost:8000 s3 mb s3://bucket2

echo Deleting bucket bucket2...
aws --endpoint-url http://localhost:8000 s3 rb s3://bucket2

echo Cleaning up temporary files...
del /q hello.foo
del /q hello.json
@echo on
