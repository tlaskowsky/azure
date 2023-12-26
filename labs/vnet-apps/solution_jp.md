# ラボの解決策

サブネットからのトラフィックのみが許可されるように、ストレージアカウントのファイアウォールルールを設定する必要があります。Webアプリはサブネット内にデプロイされていませんが、VNet統合がそれを処理します。

## ポータルで

ストレージアカウントを開きます。_ネットワーキング_で_無効_に設定し、アプリが今壊れていることを確認します。

次に_選択した仮想ネットワークとIPアドレスから有効_に切り替えてサブネットを追加します。アプリが再び動作していることを確認します。

> これらの変更が有効になるまでには1、2分かかりますが、期待される結果を確認する必要があります。

## CLIで



```
# 公開アクセスをオフにする:
az storage account update -g labs-vnet-apps --default-action Deny -n <sa-name>
```


これでアプリは壊れるはずです。



```
# IPアドレスを許可するルールを追加する:
az storage account network-rule add -g labs-vnet-apps  --vnet vnet1 --subnet subnet1 --account-name <sa-name>

# ルールを確認する
az storage account network-rule list -g labs-vnet-apps --account-name <sa-name>
```


そしてアプリは再び動作します。
