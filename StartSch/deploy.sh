#!/usr/bin/env bash

set -e

PrevDir=$PWD
cd "$(dirname "$0")"/.. # cd to the directory of the script

docker build . -t startsch -f ./StartSch/Dockerfile
mkdir -p ./StartSch/bin
docker save startsch -o ./StartSch/bin/startsch.tar
tmp=$(ssh albi@iron 'mktemp -d')
echo $tmp
rsync --progress --compress ./StartSch/bin/startsch.tar albi@iron:$tmp
echo "sudo docker load -i $tmp/startsch.tar"
echo 'sudo systemctl restart docker-startsch.service'

cd $PrevDir
