# 仮想マシン

クラウド内の仮想マシンは、データセンターやデスクトップ上の VM とほぼ同じです。それらは完全なオペレーティングシステムを実行する独立したコンピューティング環境であり、必要なものをインストールし設定するための管理者権限があります。Azure は Linux および Windows VM を実行でき、多様な事前設定されたイメージとコンピュートサイズを提供しています。

## 参照

- [Azure 仮想マシン ドキュメント](https://docs.microsoft.com/ja-jp/azure/virtual-machines/)
- [`az vm` コマンド](https://docs.microsoft.com/ja-jp/cli/azure/vm?view=azure-cli-latest)
- [`az vm image` コマンド](https://docs.microsoft.com/ja-jp/cli/azure/vm/image?view=azure-cli-latest)

## ポータルで VM を探索

ポータルを開き、新しい仮想マシンリソースを作成するために検索します。設定オプションは多数ありますが、主要なものは次のとおりです：

- イメージ - VM イメージは何を提供しますか？
- サイズ - コンピューティング容量はコストにどのように影響しますか？
- 認証とインバウンドポート - VM にどのように接続しますか？

基本オプションは OS タイプ、CPU、メモリ、接続性をカバーします。必要なオプションを確認してください - VM に到達する前にどのような他のリソースを作成する必要がありますか？

> すべてのリソースはリソース グループ内に属しています。通常、ポータルで直接関連リソースを作成できます。

_ディスク_ および _ネットワーキング_ タブを確認すると、必要に応じて VM を設定する方法がわかります：

- VM に複数のディスクを追加できます。ディスクタイプによるパフォーマンスの違いは何ですか？
- ポートレベルでネットワークアクセスを設定できます。これらのルールを適用するために作成できるオブジェクトのタイプは何ですか？

ポータルで VM を作成することはありませんが、代わりに CLI を使用します。

## CLI で Linux VM を作成

まず、新しい VM リソースが属するリソース グループを作成する必要があります。これは [リソース グループ](/labs/resourcegroups/README.md) ラボからおなじみのものです。

_グループを作成 - 好みの場所を使用してください：_



```
az group create -n labs-vm --tags courselabs=azure -l southeastasia
```


_サブスクリプションと地域に合った有効な（小さい）VM サイズを見つける：_



```
# PowerShell で：
az vm list-sizes -o table --query "[?numberOfCores<=``2`` && memoryInMB==``2048``]" --location "southeastasia"

# Bash で：
az vm list-sizes -o table --query "[?numberOfCores<=\`2\` && memoryInMB==\`2048\`]" --location "southeastasia"
```


> JMESPath に慣れるまでには時間がかかります。VM のリストをどのようにフィルタリングしていますか？

利用可能な VM サイズは、サブスクリプション、選択した地域、その地域の空き容量によって異なります。Azure の無料トライアルサブスクリプションには、有料サブスクリプションにはない制限がある場合があります。

これで、実行コストが安い小さな VM を作成できます。

📋 `vm create` コマンドを使用して Ubuntu Server VM を作成してください。指定する必要があるパラメーターがいくつかあります。

<details>
  <summary>    
    方法がわからない場合は？
  </summary>
    
ヘルプ テキストを表示：



```
az vm create --help
```


最低限必要な指定項目は：

- リソース グループ
- 場所
- VM 名
- OS イメージ

これで始められます：



```
# デフォルトが利用不可の場合がありますので、サイズを含めると良いです。
az vm create -l southeastasia -g labs-vm -n vm01 --image Ubuntu2204 --size Standard_B1ms --generate-ssh-keys
```


</details><br/>

> 新しい VM の作成には数分かかります。実行中にドキュメントをチェックして、次の質問に答えてください：

- 新しい VM の実行コストはいくらですか？
- 通常のワークロードに「A」または「B」シリーズの VM はなぜ適していないのですか？

VM が作成されたら、ポータルにアクセスしてリソース グループを開きます。VM とそのサポート リソースが表示されます。

## VM への接続

これは Linux VM なので、[SSH]() を使用して接続できます - SSH コマンドラインは MacOS、Linux、最新の Windows マシンにデフォルトでインストールされています。

📋 サーバーの IP アドレスを見つけ、`ssh` で接続してください。

<details>
  <summary>
    方法がわからない場合は？
  </summary>

`vm create` コマンドが完了すると VM の主要な詳細が表示されます。`vm show` コマンドで再度表示することができます：



```
az vm show --help
```


レスポンスに IP アドレスを含める場合は、設定するパラメーターがあることがわかります：



```
az vm show -g labs-vm -n vm01 --show-details
```

The field you want is `publicIps`. You can add a query to return just that field and store the IP address in a variable:

```
# PowerShell を使用して：
$pip=$(az vm show -g labs-vm -n vm01 --show-details --query "publicIps" -o tsv)

# Linux シェルを使用して：
pip=$(az vm show -g labs-vm -n vm01 --show-details --query "publicIps" -o tsv)
```


（または、ポータルからパブリック IP アドレスを見つけることができます。）

これで接続できます：



```
ssh-keyscan $pip > ~/.ssh/known_hosts
ssh $pip
```
</details><br/>

ユーザー名やパスワードを指定せずに接続できるはずです。

これは標準の Ubuntu Server VM です。以下のような典型的なコマンドを実行できます：

- `top` で実行中のプロセスを見る
- `uname -a` で Linux ビルドの詳細を見る
- `curl https://azure.azureauthority.in` で HTTP リクエストを行う
- `exit` で SSH セッションから抜ける

## ラボ

CLI を使用して VM のディスクの詳細を表示します。ディスクの読み書き IOPS のパフォーマンスはどうですか？次に VM を削除します - ディスクも削除されますか？

> 困ったら、[ヒント](hints_jp.md)を試すか、[解決策](solution_jp.md)をチェックしてください。

___

## クリーンアップ

このコマンドで RG を削除すると、すべてのリソースが削除されます：



```
az group delete -y -n labs-vm
```
