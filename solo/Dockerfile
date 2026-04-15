FROM node:22-slim

RUN apt-get update && apt-get install -y --no-install-recommends \
        ca-certificates \
        curl \
        jq \
        python3 \
        sudo \
        docker.io \
    && rm -rf /var/lib/apt/lists/*

# kubectl
ARG KUBECTL_VERSION=v1.32.2
RUN curl -fsSL "https://dl.k8s.io/release/${KUBECTL_VERSION}/bin/linux/amd64/kubectl" \
        -o /usr/local/bin/kubectl \
    && chmod +x /usr/local/bin/kubectl

# helm
ARG HELM_VERSION=v3.14.2
RUN curl -fsSL "https://get.helm.sh/helm-${HELM_VERSION}-linux-amd64.tar.gz" \
    | tar xz -C /usr/local/bin --strip-components=1 linux-amd64/helm

# kind
ARG KIND_VERSION=v0.29.0
RUN curl -fsSL -o /usr/local/bin/kind \
        "https://kind.sigs.k8s.io/dl/${KIND_VERSION}/kind-linux-amd64" \
    && chmod +x /usr/local/bin/kind

# solo CLI
ARG SOLO_VERSION=0.68.0
RUN npm install -g @hashgraph/solo@${SOLO_VERSION}

COPY entrypoint.sh /entrypoint.sh
COPY application.properties /config/application.properties
COPY haproxy-values.yaml /config/haproxy-values.yaml
RUN chmod +x /entrypoint.sh

ENTRYPOINT ["/entrypoint.sh"]
