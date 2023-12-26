# VNet アクセスのセキュリティ保護

バーチャルネットワークは Azure のサービスへのトラフィックを制限するのに優れており、リソースへのアクセスをセキュアにするための多くのオプションを提供します。ネットワークセキュリティグループは主要なメカニズムであり、特定のソースから特定のポートへのトラフィックを許可または拒否するルールを定義できます。

アプリケーションの異なる部分が互いにアクセスする必要がある場合は VNet を相互に接続することもでき、パブリックアクセスを許可しないネットワーク内の VM にアクセスするためにバスチオンを使用できます。

## 参考

- [ネットワークセキュリティグループ](https://learn.microsoft.com/ja-jp/azure/virtual-network/network-security-groups-overview)

- [VNet ピアリング](https://learn.microsoft.com/ja-jp/azure/virtual-network/virtual-network-peering-overview)

- [バスチオン](https://learn.microsoft.com/ja-jp/azure/bastion/bastion-overview)

- [`az network nsg` コマンド](https://learn.microsoft.com/ja-jp/cli/azure/network/nsg?view=azure-cli-latest)

## VM と NSG の作成

リソースグループ、VNet、およびサブネットの作成から始めます：



```
az group create -n labs-vnet-access --tags courselabs=azure -l eastus

az network vnet create -g labs-vnet-access -n vnet1 --address-prefix "10.10.0.0/16"

az network vnet subnet create -g labs-vnet-access --vnet-name vnet1 -n subnet1 --address-prefix "10.10.1.0/24"
```


📋 CLI の `network` コマンドを使用してネットワークセキュリティグループを作成します。

<details>
  <summary>わからない場合は？</summary>

ヘルプをチェックしてください - これによりネットワークオブジェクトのサブグループが表示されます：



```
az network --help
```


使用するグループは `nsg` です：



```
az network nsg create --help
```


名前と RG だけが必要です：



```
az network nsg create -g labs-vnet-access -n nsg01
```


</details>

NSG をポータルで開く - 新しい NSG には、すべての新しい NSG に適用されるデフォルトルールがあります：

- VNet および Azure LB からの受信を許可
- 他のすべての受信を拒否
- VNet およびインターネットへの送信を許可
- すべての送信をデフォルトで拒否

ロケーションも確認してください - コマンドでロケーションを設定しなかった場合、VNet と NSG は異なるリージョンにあるかもしれません。

**NSG が VNet と異なるリージョンにある場合、それらを関連付けることはできません。VNet と同じリージョンに新しい NSG を作成する必要があります**：


```  
az network nsg delete -g labs-vnet-access -n nsg01

az network nsg create -g labs-vnet-access -n nsg01 -l <region>
```


インターネットからポート 80 への受信トラフィックを許可する新しいルールを追加します：



```
az network nsg rule create -g labs-vnet-access --nsg-name nsg01 -n 'AllowHttp' --direction Inbound --access Allow --priority 100 --source-address-prefixes 'Internet' --destination-port-ranges '80'
```


📋 CLI を使用してサブネットに NSG をアタッチします - NSG はサブネット自体のプロパティです。

<details>
  <summary>わからない場合は？</summary>

サブネットを更新する必要があります：



```
az network vnet subnet update --help
```


NSG を名前で設定できます：



```
az network vnet subnet update -g labs-vnet-access  --vnet-name vnet1  --name subnet1 --network-security-group nsg01
```


</details>

ポータルで VNet を開いてサブネットを確認します - ここで NSG がアタッチされていることを確認できます。VNet にデプロイされたすべてのサービスはこれらの NSG ルールの対象となります。

これはポート 22 (SSH) と 3389 (RDP) がブロックされていることを意味し、この VNet で実行されている VM にはもうアクセスできません。これを行うためには別のサービスが必要になります。

## Bastion で接続

基本的な Linux VM を作成します - 今回はデフォルトの SSH キーの代わりにパスワード認証を使用します：


```
# VNet と同じ場所を使用してください：
az vm create -g labs-vnet-access -n ubuntu01 --image UbuntuLTS --vnet-name vnet1 --subnet subnet1 --admin-username labs --admin-password <strong-password> -l <region>
```


ポータルで VM を確認します - VM を作成したときに明示的に設定しなくても、_ネットワーキング_ タブに NSG がリストされています。

マシンに接続してみてください：



```
# これはタイムアウトになります - SSH はポート 22 を使用しますが、NSG ルールによってブロックされています：
ssh labs@<publicIpAddress>
```


Azure にはロックダウンされたネットワーク内の VM にアクセスするための [バスチオン](https://learn.microsoft.com/ja-jp/azure/bastion/bastion-overview) があります：

- ポータルで VM を開く
- _接続_ をクリックし、ドロップダウンから _バスチオン_ を選択
- デフォルトを使用して Azure バスチオンを作成するを選択

これには数分かかります。バスチオンサービスは VNet レベルで作成され、同じバスチオンインスタンスを VNet 内のすべての VM で使用できます。

バスチオンのセットアップが完了したら、VM を作成するときに使用した `labs` というユーザ名とパスワードを入力し、_接続_ をクリックします。ブラウザウィンドウが開き、VM へのターミナル接続が表示されますが、ポート 22 は直接アクセスに対して依然としてブロックされています。

VM セッションで Nginx Web サーバーをインストールします：



```
sudo apt update && sudo apt install -y nginx
```


VM のパブリック IP アドレスにアクセスして、ポート 80 で NSG を通じてトラフィックが許可されていることを確認します：

> `http://<publicIpAddress>`

## 第二の VNet とピアリング

VNets はアプリケーションの一部を隔離するのに適していますが、ある VNet のコンポーネントが別の VNet のコンポーネントにアクセスできるようにすることもあります。異なるリージョンに異なるサービスをホストする VNets があるかもしれません。これらの二つの VNet を _ピアリング_ を使用して Azure 内で接続できます。

📋 別のリージョンの最初の VNet と異なる IP アドレス範囲 `10.20.0.0/16` の新しい VNet および `10.20.1.0/24` の範囲の新しいサブネットを作成します。

<details>
  <summary>わからない場合は？</summary>



```
az network vnet create -g labs-vnet-access -n vnet2 --address-prefix "10.20.0.0/16" -l <region2>

az network vnet subnet create -g labs-vnet-access --vnet-name vnet2 -n subnet2 --address-prefix "10.20.1.0/24"
```


</details>

> ネットワーキングを事前に計画する必要があります。ピアリングする二つの VNet には重複しない IP アドレス範囲が必要です。

新しい VM を作成し、NSG がない新しい VNet にアタッチします。Azure は VM 用に新しい NSG を作成し、SSH トラフィックの受信を許可する追加のデフォルトルールを含みます：


```
az vm create -g labs-vnet-access -n remote01 --image UbuntuLTS --vnet-name vnet2 --subnet subnet2 -l <region2>
```


二つの VM のプライベート IP アドレスを表示します：



```
az vm list -g labs-vnet-access --show-details --query "[].{VM:name, InternalIP:privateIps, PublicIP:publicIps}" -o table
```


新しい VM に接続して、プライベート IP アドレス (10.10.1.x) を使用して最初の VM 上のウェブブラウザにアクセスできるかどうかを確認します：



```
# これは接続できます - VM の NSG はポート 22 を許可しています：
ssh <vm02-public-ip-address>

# これはタイムアウトになります：
curl <vm01-private-ip-address>
```


別のターミナルで、VNets をピアリングします - 両方のネットワークをピアリングする必要があります（これにより、アクセス権がない他人の VNet にピアリングできないようにします）：


```
az network vnet peering create -g labs-vnet-access -n vnet2to1 --vnet-name vnet2 --remote-vnet vnet1 --allow-vnet-access

az network vnet peering create -g labs-vnet-access -n vnet1to2 --vnet-name vnet1 --remote-vnet vnet2 --allow-vnet-access
```

ポータルで新しいVNetを開きます - _ピアリング_の下に、VNetsが_接続済み_の状態でピアリングされていることがわかります。これで、サブネット2（10.20で始まるアドレス）のVMは、サブネット1（10.10で始まるアドレス）のVMに到達できます。

2番目のVMのSSHセッションで、もう一度最初のVMにアクセスしてみてください：



```
# これで動作します:
curl <vm01-private-ip-address>

# IPアドレスを確認します - 10.20のアドレスだけが見えます:
ip a 
```


> ピアリングはVMに新しいNICを追加しませんが、ネットワーク間のルーティングを処理します。

## ラボ

これで、サブネット1のWebサーバーはインターネット上の任意のマシンからアクセス可能になり、サブネット2のVMも同様です。しかし、サブネット2のVMはサブネット1のVM上の任意のポート、SSHも含めてアクセスできてしまいます - これは望ましくありません。サブネット2のマシンからポート80のWebサーバーへのトラフィックのみが許可されるようにNSGルールを更新してください。

> 詰まったら[ヒント](hints_jp.md)を試すか、[解決策](solution_jp.md)を確認してください。

___

## クリーンアップ

このコマンドでRGを削除すると、すべてのリソースが削除されます：


```
az group delete -y --no-wait -n labs-vnet-access
```
