# ラボの解決策

広い出力でPodをリストアップすると、内部IPアドレスが表示されます：


```
kubectl get pods -o wide
```


> Podのアドレスは、サブネットに指定した範囲内にすべてあります。

これは、Azureネットワークプラグインを使用したためであり、個々のPodがKubernetesの外部のサービスからvnet全体に対してアドレス指定可能であることを意味します。

AzureポータルでApplication Gatewayを開きます：

- _フロントエンドIP構成_ には、DNS名が付いたパブリックIPアドレスが表示されます
- _ヘルスプローブ_ の下では、`pb-default-whoami-internal-http-whoami`のような名前のプローブがあり、Podがどのように健康状態をテストされているかが表示されます
- _バックエンドプール_ の下には、`pool-default-whoami-internal-http-bp-80`のようなエントリがあります
- そのプールを選択すると、PodのIPアドレスが表示されます

Application GatewayはAKSと同じvnet内で動作しているため、内部IPアドレスを使用してPodに到達することができます。
