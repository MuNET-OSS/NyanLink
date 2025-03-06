FROM eclipse-temurin:21-jre-alpine

# Install bash, curl
RUN apk add --no-cache bash curl

# Create user worldlinkd
RUN addgroup -S worldlinkd && adduser -S worldlinkd -G worldlinkd

WORKDIR /app/wl
COPY docs/* .

CMD ["/bin/bash", "/app/wl/docker_entry.sh"]
