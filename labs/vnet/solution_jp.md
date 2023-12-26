# ラボ解決策

私のリソースグループからエクスポートされたARMファイルは以下の通りです：

- [exported/template.json](./lab/exported/template.json) - リソース名などのデフォルトパラメータ値を持つリソース定義
- [exported/parameters.json](./lab/exported/parameters.json) - 新しいデプロイメントのために変更可能な一連のパラメータ。

テンプレートは、リソースがグローバルに一意の名前を使用していないため、新しいRGで再利用できます。リソースは異なるRGで同じ名前を持つことができますが、公開IPアドレスは再利用できません。それは既に既存のVMに割り当てられているからです。

デプロイを試みて、うまくいくか確認してください：



```
az group create -n labs-vnet2 --tags courselabs=azure -l eastus

az deployment group create --resource-group labs-vnet2 --name deploy1 --template-file ./labs/vnet/lab/exported/template.json
```


IDが許可されていないというエラーが表示されるはずです - それを修正し、`requireGuestProvisionSignal` フィールドがサブスクリプションで有効ではないという別のエラーが表示されるかもしれません。

- [updated/template.json](./lab/updated/template.json) - エクスポートされたテンプレートの問題を修正

> このテンプレートをデプロイしたい場合は、自分のエクスポートされたテンプレートからSSHの詳細を上書きする必要があります - それは `az vm create` コマンドで作成されたキーを参照しています。



```
az deployment group create --resource-group labs-vnet2 --name deploy1 --template-file ./labs/vnet/lab/updated/template.json
```


これでうまくいくはずです - そしてそれは繰り返し可能です。同じコマンドを再び実行してもリソースは変わりません。

新しいRGのVMは、ARMテンプレートで指定されたものとは異なる新しい公開IPアドレスを持っています。

新しいIPアドレスを取得し、接続できます：



```
az vm show -g labs-vnet2 -n vm01 --show-details --query publicIps -o tsv

ssh <vm01-public-ip>

# VM内部で：
ip address

exit
```


新しいVMは他のVMと同じプライベートIPアドレスを持つべきですが、異なるRGの異なるvnetにあります。
