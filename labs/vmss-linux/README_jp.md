# VM スケールセット - Linux 用のカスタム イメージ

VM スケールセット内でカスタム イメージを使用すると、インスタンスはオンラインになるとすぐに作業を開始できます。ただし、新しいアプリケーションのリリースごとに新しいイメージを作成する必要があるため、アップグレードは複雑になります。アプリケーションがベースの VM イメージに迅速に展開できる場合、展開をスクリプト化できます。

このラボでは、_cloud-init_ システムを使用せずに Linux VMSS にアプリケーションを展開し、インスタンスが起動したときに設定する方法を示します。また、新しい VMSS のロードバランサー ルールを自動化する手順も説明します。

## 参考資料

- [Azure Linux VM 用の cloud-init サポート](https://docs.microsoft.com/en-us/azure/virtual-machines/linux/using-cloud-init)

- [cloud-init 構成の例](https://cloudinit.readthedocs.io/en/latest/topics/examples.html#)

- [`az network lb rule` コマンド](https://learn.microsoft.com/en-us/cli/azure/network/lb/rule?view=azure-cli-latest)

## cloud-init を使用して VM を作成する

VM スケールセットは、アプリケーションをスケールで実行するために非常に便利ですが、アプリケーションを初めて設定する場合は、通常、単一の VM から始めて設定を確認する方が簡単です。

まず、このラボのために新しいリソース グループを作成します：




```
az group create -n labs-vmss-linux --tags courselabs=azure -l southeastasia
```


cloud-init は、新しい仮想マシンを構成するための強力なクロスプラットフォーム システムです。事前条件の展開、アプリケーションのインストール、構成ファイルの作成など、通常の手順をすべて実行できます。

- [cloud-init.txt](/labs/vmss-linux/setup/cloud-init.txt) - これは Nginx ウェブサーバーをインストールするシンプルな初期化スクリプトです

VM を作成する際に、カスタム データ スクリプトとしてファイル `labs/vmss-linux/setup/cloud-init.txt` を渡すことで、cloud-init スクリプトを含めることができます。

📋 `UbuntuLTS` イメージから新しい VM を作成し、カスタムデータ スクリプトとしてファイル `labs/vmss-linux/setup/cloud-init.txt` を渡します。

<details>
  <summary>わからない場合は？</summary>

`az` コマンドでローカル ファイルを参照する場合、`@<file-path>` 構文を使用できます：


```
# 使用可能なサイズを使用してください：
az vm create -l southeastasia -g labs-vmss-linux -n web01 --image UbuntuLTS --size Standard_A1_v2 --custom-data @labs/vmss-linux/setup/cloud-init.txt --public-ip-address-dns-name <your-dns-name> --generate-ssh-keys
```


</details><br/>

VM が作成されたら、cloud-init スクリプトの出力を表示するためのコマンドを実行します。ログ ファイルは標準的なパスに書き込まれます：



```
az vm run-command invoke  -g labs-vmss-linux -n web01 --command-id RunShellScript --scripts "cat /var/log/cloud-init-output.log"
```


> Nginx のインストールログが表示されます

また、別の run コマンドを使用して、ウェブサーバーがリッスンしていることをテストできます：



```
az vm run-command invoke  -g labs-vmss-linux -n web01 --command-id RunShellScript --scripts "curl localhost"
```

## Linux VMSS で cloud-init を使用

今、cloud-init がどのように機能するかを見てみました。次に、VM スケールセットで使用するより興味深いセットアップ スクリプトがあります。

- [cloud-init-custom.txt](/labs/vmss-linux/setup/cloud-init-custom.txt) - Nginx をインストールし、カスタム HTML ページを書き込みます。cloud-init を使用すると、ファイルに変数を挿入できます。この例では、VM ホスト名をウェブページに挿入しています。

VMSS で cloud-init ファイルを使用するのは同じカスタムデータのアプローチです。

📋 `cloud-init.txt` ファイルをカスタムデータ スクリプトとして渡して、Ubuntu イメージから 3 つのインスタンスを持つ VM スケールセットを作成します。

<details>
  <summary>わからない場合は？</summary>

VMSS に対するコマンドは、VM に対するものとほぼ同じですが、インスタンス数を追加するだけです：



```
az vmss create -n vmss-web01 -g labs-vmss-linux --vm-sku Standard_D2s_v5 --instance-count 3 --image UbuntuLTS --custom-data @labs/vmss-linux/setup/cloud-init-custom.txt --public-ip-address-dns-name <unique-dns-name>
```


</details><br/>

VMSS で新しい VM スケールセットが PIP とロード バランサーで作成されることを [VMSS Windows ラボ](/labs/vmss-win) で見ましたが、ロード バランサーのルールは設定されていないため、トラフィックはどこにも送信されません。

ルールのリストを表示して、設定が何もないことを確認します：



```
az network lb list -g labs-vmss-linux -o table

az network lb rule list -g labs-vmss-linux -o table --lb-name <lb-name>
```

📋 ポート 80 にフォワーディングするロード バランサー ルールを作成します。また、同じポートのヘルス プローブも必要です。

<details>
  <summary>わからない場合は？</summary>

まず、ヘルス プローブを作成します：



```
az network lb probe create -g labs-vmss-linux -n 'http' --protocol tcp --port 80  --lb-name <lb-name> 
```


その後、新しいルールの参照に使用できるようにします：



```
az network lb rule create -g labs-vmss-linux --probe-name 'http' -n 'http' --protocol Tcp --frontend-port 80 --backend-port 80 --lb-name <lb-name> 
```          


</details><br/>

> PIP の IP アドレスまたは DNS 名に移動して、出力を確認してください。ウェブページは、マシンのローカル名を表示するカスタム HTML ページであるはずです。

ブラウザのキャッシュで負荷分散が機能しないことがあるため、コマンドラインで確認できます：



```
curl http://<pip-address>
```


コマンドを繰り返すと、すべての 3 つのインスタンスからの応答が表示されるはずです。

## VMSS の更新

VM スケールセットには、アプリケーションの更新を管理する機能が備わっています。VMSS [モデル](https://learn.microsoft.com/en-us/azure/virtual-machine-scale-sets/virtual-machine-scale-sets-upgrade-scale-set) はインスタンスの所望の状態を保存し、モデルが変更されるたびに各インスタンスをアップグレードできます。

VMSS インスタンスの詳細を表示し、`latestModelApplied` フィールドで最新の適用モデルを確認できます：



```
az vmss list-instances -g labs-vmss-linux -n vmss-web01
```


VMSS を所望の VM の状態を変更することで更新できます。これによりモデルが変更され、既存の VM は最新でなくなります。cloud-init スクリプトを使用してカスタムデータを異なるものに変更する場合の動作を見てみましょう：

- [cloud-init-updated.txt](/labs/vmss-linux/setup/cloud-init-updated.txt) - Nginx が返す HTML ファイルを変更します。

カスタムデータの更新は、ファイルを設定する必要があるため、通常よりも簡単ではありません。この場合、CLI はファイルを使用できないため、ファイルを Base-64 文字列に読み込む必要があります：



```
# PowerShell で
$customData=$(cat labs/vmss-linux/setup/cloud-init-updated.txt | base64)

# または Bash で：
customData=$(cat labs/vmss-linux/setup/cloud-init-updated.txt | base64)
```


**注意:** *以下のコマンドは Windows PowerShell で使用してください*



```
$data=cat labs/vmss-linux/setup/cloud-init-updated.txt

$customData=[Convert]::ToBase64String([Text.Encoding]::UTF8.GetBytes($data)

[Text.Encoding]::Utf8.GetString([Convert]::FromBase64String($customData))
```


次に、Base-64 文字列を更新コマンドに渡して、VM のカスタムデータ フィールドとして設定します：



```
az vmss update -g labs-vmss-linux -n vmss-web01 --set virtualMachineProfile.osProfile.customData=$customData
```


> コマンドが完了した後、インスタンスリストを表示するか、ポータルでインスタンスを確認してください。

インスタンスは最新のモデルが適用されていないことを示しています。**モデルの変更は既存の VM に自動的に適用されない**ため、スケールアップすると新しい VM が最新のモデルを持っていることがわかりますが、古い VM は明示的に更新する必要があります。

📋 `az vmss` コマンドを使用してすべてのインスタンスを更新します。

<details>
  <summary>わからない場合は？</summary>

すべてのサブコマンドをリストします：



```
az vmss --help
```


これが必要なコマンドです。特定のインスタンスを更新するか、すべてのインスタンスを更新できます：



```
az vmss update-instances  -g labs-vmss-linux -n vmss-web01 --instance-ids '*' 
```


</details><br/>

インスタンスを再確認すると、すべてのインスタンスが最新のモデルを使用しているはずです。ただし、ウェブページに移動すると、HTML ページが変更されていないことがわかります。カスタムデータファイルの内容は更新されましたが、このファイルはプロビジョニング時にのみ処理されるため、これらの VM はセットアップを再実行しません。

VMSS をスケールアップします。新しいインスタンスがオンラインになると、新しいモデルを使用して新しいコンテンツをプロビジョニングします：



```
az vmss scale -g labs-vmss-linux -n vmss-web01 --new-capacity 5
```


いくつかの curl リクエストを行うと、古いマシンと新しいマシンから異なる応答が表示されるはずです。

## ラボ

VMSS は悪い状態にあります。すべての VM は最新のモデルであり、すべてが健康であり、ロード バランサーの有効なターゲットです。ただし、アプリケーションの異なるバージョン（シンプルな HTML ページ）が実行されています。これを修正するには、古い VM を現在のモデルから再作成する必要があります。これはポータルまたは CLI で行うことができます。

> 行き詰まっていますか？[ヒント](hints_jp.md)を試すか、[ソリューション](solution_jp.md)を確認してください。

___

## クリーンアップ

ラボのリソース グループを削除します：



```
az group delete -y -n labs-vmss-linux
```
