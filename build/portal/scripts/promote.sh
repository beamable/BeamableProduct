#!/bin/bash -ex

: ${PORTAL_ENV:=dev}
: ${BUCKET_PREFIX:=disruptor-engine}
: ${BUILD_NUMBER:=0}

export DEST_BUCKET=${BUCKET_PREFIX}-${PORTAL_ENV}

echo "Building dist..."
docker-compose down
docker-compose up --build

echo "Uploading to S3 bucket 's3://${DEST_BUCKET}'..."
aws s3 sync ../../portal/dist/ s3://${DEST_BUCKET}/${BUILD_NUMBER}/ 
aws s3 cp ../../portal/dist/index.html s3://${DEST_BUCKET}/index${BUILD_NUMBER}.html
aws s3api put-bucket-website --bucket ${DEST_BUCKET} --website-configuration "{
    \"IndexDocument\": {
        \"Suffix\": \"index${BUILD_NUMBER}.html\"
    },
    \"ErrorDocument\": {
       \"Key\": \"index${BUILD_NUMBER}.html\"
    }
}"