# ラボ解決策

YAMLファイルにはACI名が含まれているため、それを再デプロイしても何も起こりません :) 名前付きインスタンスに更新がないため、そのまま残されます。

CLIで名前をオーバーライドしてみることができます：



```
# これは失敗します:
az container create -g labs-aci-compose -n assetmanager2 --file labs/aci-compose/assetmanager-aci.yaml
```


それは機能しないので、新しい名前（または名前なし）のYAMLのコピーを作成してデプロイする必要があります：

- [lab/assetmanager2-aci.yaml](/labs/aci-compose/lab/assetmanager2-aci.yaml) - 名前を除いて同じ仕様

そのファイルを**編集して**接続の詳細を追加する必要があります、その後デプロイできます：



```
az container create -g labs-aci-compose -n assetmanager2 --file labs/aci-compose/lab/assetmanager2-aci.yaml
```


新しいインスタンスのIPアドレスにアクセスしてみてください - 同じアプリです。Azure Files共有を開くと、新しいロックファイルが見えます。各インスタンスは自分のものを書きます。

別々のIPアドレスは、DNSサービスがそれらの間でロードバランシングをサポートしている場合を除き、あまり役に立ちません。または、それぞれにDNS名を追加し、トラフィックを分散させるためにトラフィックマネージャープロファイルを作成することもできます。
