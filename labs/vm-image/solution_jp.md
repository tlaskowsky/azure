# ラボの解決策

- _Traffic Manager Profile_ を作成し、ユニークなDNS名を付けます。

- トラフィックマネージャーを開き、_エンドポイント_ へ移動します。

- VMのパブリックIPアドレスの1つをエンドポイントとして追加します。

> PIPにDNS名がないため、エラーが表示されます。

DNS名は必要です。なぜならトラフィックマネージャーはPIP間のロードバランシングに単にラウンドロビンDNSを使用するからです。

- すべてのVM PIPにDNS名を追加します。

- それぞれのPIPをトラフィックマネージャーのエンドポイントとして追加します。

トラフィックマネージャーのURLにアクセスしてみてください。1つのVMからの応答が表示されますが、リフレッシュすると同じVMからの応答が得られることがあります（HTTPスタックには多くのキャッシュがあります）。

MacやLinuxマシンをお持ちの場合、アドレスのDNS応答を表示する`dig`ツールが利用可能です。トラフィックマネージャーのアドレスをチェックし、ロードバランシングがどのように行われているかを確認してください：



```
# 例えば、私のプロファイルの場合：
dig labsvmimagees.trafficmanager.net
```


私の場合はこのような応答があります：



```
;; ANSWER SECTION:
labsvmimagees.trafficmanager.net. 60 IN CNAME   labspipn0.southeastasia.cloudapp.azure.com.
labspipn0.southeastasia.cloudapp.azure.com. 10 IN A 52.148.241.50
```


そして、繰り返すと、アドレス間でのロードバランシングを提供するために異なる応答が表示されます。
