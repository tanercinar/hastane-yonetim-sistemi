# syntax=docker/dockerfile:1

# MinIO Community Edition became source-only. Build the final upstream security
# release instead of pinning the known-vulnerable legacy binary container image.
FROM golang:1.24.8-alpine3.22 AS build

ARG MINIO_VERSION
ENV CGO_ENABLED=0

RUN test -n "${MINIO_VERSION}"
RUN go install -trimpath "github.com/minio/minio@${MINIO_VERSION}"

FROM alpine:3.22

ARG MINIO_VERSION

LABEL org.opencontainers.image.title="Hospital Management local MinIO" \
      org.opencontainers.image.description="Local-only MinIO build from pinned upstream source" \
      org.opencontainers.image.source="https://github.com/minio/minio" \
      org.opencontainers.image.version="${MINIO_VERSION}" \
      org.opencontainers.image.licenses="AGPL-3.0-only"

RUN apk add --no-cache ca-certificates \
    && addgroup --system --gid 10001 minio \
    && adduser --system --uid 10001 --ingroup minio --home /home/minio minio \
    && mkdir -p /data \
    && chown minio:minio /data

COPY --from=build --chown=minio:minio /go/bin/minio /usr/local/bin/minio

USER minio:minio
EXPOSE 9000 9001
VOLUME ["/data"]

ENTRYPOINT ["/usr/local/bin/minio"]
CMD ["server", "/data", "--console-address", ":9001"]
