FROM node:10-alpine

RUN npm install -g json
ADD build/docker/scripts /scripts

WORKDIR /Beamable
ENTRYPOINT sh /scripts/promote.sh