# DAPR Pluggable Component For LiteDb

[![Build Status](https://github.com/rosenkolev/dapr-pluggable-component-litedb/actions/workflows/dotnet.yml/badge.svg)](https://github.com/rosenkolev/dapr-pluggable-component-litedb/actions/workflows/dotnet.yml)
[![Docker Image Version](https://img.shields.io/docker/v/rosenkolev/dapr-pluggable-component-litedb)](https://hub.docker.com/r/rosenkolev/dapr-pluggable-component-litedb)
[!![Docker Image Size](https://img.shields.io/docker/image-size/rosenkolev/dapr-pluggable-component-litedb)](https://hub.docker.com/r/rosenkolev/dapr-pluggable-component-litedb)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://github.com/rosenkolev/dapr-pluggable-component-litedb/blob/main/LICENSE)

[Report Bug](/issues) . [Request Feature](/issues)

<details>
<summary>Table of Contents</summary>

1. [About The Project](#about-the-project)
   * [Features](#features)
1. [Getting Started](#getting-started)
   * [Dependencies](#dependencies)
   * [Installation](#installation)
1. [Usage](#usage)
   * [Configure](#configure)
   * [Docker Compose](#docker-compose) 
1. [Contributing](#contributing)
1. [License](#license)

</details>

## About The Project

**Pluggable component to use [LiteDb](https://www.litedb.org/) as [DAPR](dapr.io) state.**

### Features

Features are based on the DAPR state protocol (see [Dapr State Stores](https://docs.dapr.io/reference/components-reference/supported-state-stores/)).

| Component |	CRUD | Transactional | Batch | ETag | TTL | Actors | Query |
| --------- | ---- | ------------- | ----- | ---- | --- | ------ | ----- |
| litedb    | ✅  | ✅            | ✅   | ⬜   | ⬜ | ⬜     | ✅   | 

## Getting Started

### Dependencies

[![NuGet Version](https://img.shields.io/nuget/dt/LiteDb?style=flat-square&label=LiteDB)](https://www.nuget.org/packages/LiteDB) &nbsp;
[![NuGet Version](https://img.shields.io/nuget/dt/Dapr.PluggableComponents.AspNetCore?style=flat-square&label=Dapr.PluggableComponents.AspNetCore)](https://www.nuget.org/packages/Dapr.PluggableComponents.AspNetCore)

### Installation

```shell
dotnet run -d -v dapr-sockets:/tmp/dapr-components-sockets:rw rosenkolev/dapr-pluggable-component-litedb
```

## Usage

### Configure

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
    #   value: default
    # - name: indexes
    #   value: 
```

#### Spec metadata fields

| Field          | Required | Details | Example |
| -------------- | -------- | ------- | ------- |
| databaseName   | N        | The database file name. <br> Default: `state` | `"my_database"` |
| collectionName | N        | The collection name <br> Default: `default` | `"roles"`, `"users"` |
| indexes        | N        | The collection indexes | `"email"` <br> `"email,externalId"` |

#### Files

| Path | Details |
| ---- | ------- |
| `/.db/{databaseName}.db` (ie: `/.db/state.db`) | The database file. |
| `/.db/metadata.json` | The file preserving components metadata between container restarts. |

### Docker Compose

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

## Contribution

The project is open for contribution by the community.

<a href="https://github.com/dapr/components-contrib/graphs/contributors">
  <img src="https://contributors-img.web.app/image?repo=rosenkolev/dapr-pluggable-component-litedb" />
</a>

## License

Project is licensed under the [MIT](LICENSE.TXT) license.
