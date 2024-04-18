# DAPR Pluggable Component For LiteDb

Use LiteDb as DAPR state

## Features

- state
- query
- transaction state
- bulk state

## Configure

```yaml
apiVersion: dapr.io/v1alpha1
kind: Component
metadata:
  name: roles
spec:
  type: state.litedb
  version: v1
  metadata:
    # - name: databaseName
    #   value: state
    # - name: collectionName
    #   value: roles
    # - name: indexes
    #   value: email
```

## Docker Compose

```yaml
version: '3.4'

volumes:
  dapr-sockets:

services:
  api:
    image: ${DOCKER_REGISTRY-}api
    build:
      context: .
      dockerfile: Api/Dockerfile
  api-dapr:
    image: "daprio/daprd"
    container_name: api-darp
    command: ["./daprd",
      "-app-id", "api",
      "-app-port", "8080",
      "-placement-host-address", "placement:50006",
      "--resources-path", "/components"]
    volumes:
      - dapr-sockets:/tmp/dapr-components-sockets:rw
      - ./dapr/components/:/components
    depends_on:
      - dapr-litedb
    network_mode: "service:api"
  
  dapr-litedb:
    image: rosenkolev/dapr-pluggable-component-litedb
    container_name: dapr-litedb
    restart: no
    volumes:
      - dapr-sockets:/tmp/dapr-components-sockets:rw
```
