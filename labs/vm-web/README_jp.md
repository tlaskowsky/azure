# 仮想マシン - ウェブサーバー

クラウド内で動作する仮想マシン (VM) は、24時間365日利用可能である必要があるウェブサーバーのようなワークロードを実行するためのシンプルな方法です。ウェブサーバーには、公開 IP アドレスやアクセス用の DNS 名など、他にも要件があります。

このラボでは、Linux VM を作成し、VM に接続して必要なパッケージを手動でデプロイすることでウェブサーバーを設置する方法を見ていきます。

## 参照資料

- [Azure の公開 IP アドレス](https://learn.microsoft.com/ja-jp/azure/virtual-network/ip-services/public-ip-addresses)

- [`az network public-ip` コマンド](https://learn.microsoft.com/ja-jp/cli/azure/network/public-ip?view=azure-cli-latest)

- [`az vm image` コマンド](https://docs.microsoft.com/ja-jp/cli/azure/vm/image?view=azure-cli-latest)


## ポータルでの VM の探索

ポータルを開き、新しい仮想マシンリソースを作成するために検索します。_ネットワーキング_ タブで公開 IP を指定できます：

- 新しいリソースである Public IP Address (PIP) を作成する必要があります
- 実際の IP アドレスは選べませんが...
- ネットワーク セキュリティ グループ (NSG) も設定できます
- VM が削除されるときに PIP と NSG も削除するように設定できます

## 公開 DNS 名を持つ Linux VM の作成

まず、新しい VM リソースが配置されるリソースグループを作成する必要があります。これは [リソースグループ](/labs/resourcegroups/README.md) ラボからお馴染みの内容です：

_グループを作成します - 好きな場所を選んでください：_



```
az group create -n labs-vm-web --tags courselabs=azure -l southeastasia
```

📋CLI を使用して Ubuntu Server VM を作成します。VM にアクセスするための一意の公開 DNS 名を指定します。

<details>
  <summary>わからない場合</summary>

ヘルプ テキストを確認します：



```
az vm create --help
```


`public-ip-address-dns-name` というパラメーターがあり、これを使用して DNS 名を設定できます：





```
# 利用可能なサイズを覚えておいてください：
az vm create -l southeastasia -g labs-vm-web -n vm01 --image Ubuntu2204 --size Standard_B1ms --generate-ssh-keys --public-ip-address-dns-name <your-dns-name> 
```


</details><br/>

DNS 名は VM が使用する PIP (公開 IP アドレス) にアタッチされます。PIP は独自のライフサイクルを持ち、CLI を使用して VM から独立して管理できます：

_リソースグループ内のすべての PIP をリストアップします：_



```
az network public-ip list -o table -g labs-vm-web
```

📋 VM に使用されている PIP の詳細を表示して、FQDN（完全修飾ドメイン名）を確認します。

<details>
  <summary>
    わからない場合
  </summary>



```
az network public-ip show -g labs-vm-web -n <your-pip-name> -o json
az network public-ip show -g labs-vm -n <your-pip-name> --query "{fqdn: dnsSettings.fqdn,address: ipAddress}"
```


</details><br/>

> FQDN は `[vm-name].[region].cloudapp.azure.com` の形式になります。例えば、私の場合は `azureauth-web.southeastasia.cloudapp.azure.com` です。

FQDN を使用して VM に接続できます - 実際の IP アドレスが変更されても FQDN は一定です。

## VM にウェブサーバーをインストールする

SSH と DNS 名を使用して VM に接続します：



```
ssh-keyscan <Public IP of VM> > ~/.ssh/known_hosts
ssh <your-fqdn>
```


次に、Nginx ウェブサーバーをインストールします：



```
sudo apt update && sudo apt install -y nginx

# インストールが完了したら、ブラウズできるか確認します：
curl localhost
```


> ウェブブラウザを開いて、マシンの FQDN にアクセスします。http://[vm-name].[region].cloudapp.azure.com

VM からウェブサイトにアクセスできますが、外部からはアクセスできません。これはネットワーク セキュリティ グループ (NSG) によるものです。NSG はデフォルトで作成され、VM にアタッチされます。これは、受信トラフィックをブロックするように設定されたファイアウォールのようなものです。

トラブルシューティングを行う際には、CLI よりもポータルがより有用なことが多いです。

📋 ポータルにアクセスして、VM の NSG を見つけ、ポート 80 への受信トラフィックを許可するように設定を変更します。

<details>
  <summary>
    わからない場合
  </summary>

ポータルでリソースグループを開き、NSG を開きます。これは `[vm-name]NSG` という名前になります：

- _概要_ ページで受信ルールを見ることができます
- ポート 22 は許可されています（SSH 接続用）およびいくつかの 65000+ ポート
- 他のすべてのポートはブロックされています
- _受信セキュリティ ルール_ ページを開きます
- 任意のソースからの HTTP トラフィックを許可する新しいルールを追加します

</details><br/>

VM の FQDN をブラウザでリフレッシュすると、Nginx のウェルカムページが表示されます。

## VM の停止と開始

VM は実行中である限り課金されます。作業が終わったが後で VM を維持したい場合、VM を停止することができます - これにより、既存の状態が維持されます。

**ただし、停止した VM も課金されます。** VM の課金を停止するには、それを _割り当て解除_ する必要があります。

📋 CLI を使用して VM を割り当て解除します。完了したら、VM が使用していた公開 IP アドレスの詳細を確認します。

<details>
  <summary>わからない場合</summary>

VM に関連するすべての利用可能なコマンドを印刷し、その後 `stop` の詳細に焦点を当てます：



```
az vm --help

az vm deallocate --help
```

VM を停止して割り当て解除するには、次のコマンドを実行します：



```
az vm deallocate -g labs-vm-web -n vm01
```


その後、PIP を確認します：



```
az network public-ip show -g labs-vm-web -n vm01PublicIP
```


</details><br/>

> PIP に割り当てられた IP アドレスはなくなります - VM を割り当て解除すると、その IP アドレスは別のユーザーに自由になります。

VM を再起動すると、PIP に新しい公開 IP アドレスが割り当てられることがわかりますが、FQDN を使用してウェブサイトに引き続きアクセスできます。Azure は新しい IP アドレスを指すように DNS レコードを設定します。

## ラボ

通常、動的 IP アドレスで問題はありません - どのみち DNS エントリがルーティングされますが、VM を割り当て解除しても維持される固定 IP アドレスが必要な場合もあります。

PIP を VM に割り当てられていなくても固定 IP アドレスを使用するように設定できます。PIP を一定の IP アドレスを使用するように変更し、VM を開始、停止、割り当て解除したときにアドレスが維持されるか確認します。

> 困ったら、[ヒント](hints_jp.md)を試すか、[解決策](solution_jp.md)をチェックしてください。

___

## クリーンアップ

このコマンドで RG を削除し、すべてのリソースを削除します：



```
az group delete -y -n labs-vm-web
```
