# IaaS - アプリケーションの自動デプロイ

IaaSアプローチでは、VMに手動でログインしアプリケーションをデプロイする必要はありません。自動化を利用することができます。ARMテンプレートやBicepを使用してインフラストラクチャをモデル化し、VMセットアップにデプロイスクリプトを含めることができます。これらのスクリプトは、アプリケーションの依存関係を自動的にインストールし、アプリケーション自体を設定し、設定ファイルをセットアップします。

このラボでは、Bicepを使用してWindows VMとSQL Serverデータベースを含むデプロイメントを定義し、.NETアプリケーションをVMにデプロイします。

## コアリソース

Bicepは複数のファイルにわたってモデルを分割することで、より大きなインフラストラクチャの要件をサポートします。ファイル間で変数名を共有することで、異なるBicepファイルで定義されたリソースを参照できます。

- [templates/vars.json](/labs/iaas-bicep/templates/vars.json) - 変数の定義のみで、これらはBicepファイルで参照されるリソースの名前です。これは単なるJSONです。

- [templates/core.bicep](/labs/iaas-bicep/templates/core.bicep) - `loadJsonContent`関数を使って変数ファイルを参照し、仮想ネットワーク、サブネット、NSGなどのコアリソースを定義します。

このBicepファイルを見る価値があります。十分に小さく、理解しやすいですが、いくつかの便利な機能が使われています。

- デプロイする場所のパラメータがありますが、オプションです。デフォルト値はどこから来るのでしょうか？

- このファイルの外部で定義されているすべてのリソース名はどこで設定され、どのようにこのファイルはそれらを参照しますか？

- 親/子関係を持つオブジェクトはネストされて定義されていません。このファイルは読みやすくなっています。関係はどのように指定されていますか？

📋 `labs-iaas-bicep`というリソースグループを作成し、コアBicepファイルをデプロイしてください（[Bicepラボ](/labs/arm-bicep/README_jp.md)でこれについて説明しました）。

<details>
  <summary>わからない場合はこちら</summary>

これは簡単です - `group create`と`deployment group create`を使用します。



```
az group create -n labs-iaas-bicep --tags courselabs=azure -l southeastasia 

az deployment group create -g labs-iaas-bicep --name core --template-file labs/iaas-bicep/templates/core.bicep
```


</details><br/>

作成されたリソースを確認してください。



```
az resource list -g labs-iaas-bicep -o table
```



> NSGと仮想ネットワークが表示されますが、サブネットは表示されません。これはVNetの子リソースだからです。

## SQLサーバー

SQLサーバーデータベースリソースを定義する2番目のBicepファイルがあります。

- [templates/sql.bicep](/labs/iaas-bicep/templates/sql.bicep) - 共有変数JSONファイルを読み込みながら、SQLサーバーとデータベースを定義します。

このテンプレートにもいくつかの良い点があります。

- ほとんどのパラメータにはデフォルト値がありますが、SQLサーバー名がすでに使用されていないDNS名であることをテンプレートはどのように確認しますか？

- 管理者パスワードは必須ですが、特別な扱いを受けています。`secure`フラグは何をしますか？

- SQLサーバーは、コアBicepファイルで定義されたサブネットに作成されます。このファイルはどのようにしてここに定義されていないリソースを参照しますか？

ARMデプロイメントには2つのモードがあります。

- _完全_ - デプロイしているテンプレートにすべてのリソースの完全な定義が含まれている場合

- _増分_ - テンプレートがモデルの一部を定義し、他のリソースに追加されることを期待している場合

デフォルトモードは _完全_ なので、SQL Bicepファイルをデプロイしようとすると問題があります。

`what-if`フラグを使用して、デプロイメントで何が起こるかを確認してください。


```
az deployment group create -g labs-iaas-bicep --name sql --template-file labs/iaas-bicep/templates/sql.bicep --mode complete --what-if --parameters sqlAdminPassword=<sql-password>
```


> ARMはここで混乱します :) テンプレートに指定されていないためVNetを削除すると言いますが、その後削除されたサブネットを参照しようとします...

📋 SQL BicepファイルをコアBicepリソースと同じRGにインクリメンタルデプロイとしてデプロイしてください。

<details>
  <summary>わからない場合はこちら</summary>

複数のBicepファイルにデプロイメントを分割する場合は、インクリメンタルモードを使用する必要があります。



```
az deployment group create -g labs-iaas-bicep --name sql --template-file labs/iaas-bicep/templates/sql.bicep --mode incremental --parameters sqlAdminPassword=<sql-password>
```


</details><br/>

もう一度リソースをリストしてください。コアリソースは削除されず、新しいものが追加され、SQLサーバー名はランダムな文字列になっています。



```
az resource list -g labs-iaas-bicep -o table
```


SQLサーバーの子リソースの1つはVNetルールです。これは、同じ仮想ネットワーク内の任意のリソースにSQLサーバーへのアクセスを許可するファイアウォール設定です。ルールが適用されているか確認してください。



```
az sql server vnet-rule list -g labs-iaas-bicep --server <sql-server>
```

## Windows アプリケーション VM

このアプリケーション用の最終的なBicepファイルは、Windows Server VMを定義します。デプロイするアプリケーションは、[IaaS apps lab](/labs/iaas-apps)で使用したものと同じですが、すべてのステップはここで自動化されています：

- [templates/vm.bicep](/labs/iaas-bicep/templates/vm.bicep) - VMと必要なリソース（NICとPIP）を含み、共有されたJSON変数ファイルを介してCoreリソースを参照します。

このテンプレートからさらに詳しく解析すると：

- オブジェクト名は他のオブジェクト名から派生していますが、SQL Serverの名前はSQL Bicepファイルから繰り返されます。それにはどのような利点と欠点がありますか？

- BicepリソースはIaaSコンポーネントを定義していますが、VM作成後にPowerShellスクリプトを実行する追加リソースがあります。これは、[scripts/vm-setup.ps1](/labs/iaas-bicep/scripts/vm-setup.ps1)のステップを実行してアプリケーションをデプロイします。

📋 RGにVM Bicepファイルをインクリメンタルデプロイメントとしてデプロイしてください。予期しないことが起こらないようにします。

<details>
  <summary>不明な場合</summary>

what-ifデプロイメントを実行してください：



```
az deployment group create --what-if -g labs-iaas-bicep --name vm --template-file labs/iaas-bicep/templates/vm.bicep --mode incremental --parameters adminPassword=<vm-password> sqlPassword=<sql-password>
```


問題がなければ、デプロイを続けてください：



```
az deployment group create -g labs-iaas-bicep --name vm --template-file labs/iaas-bicep/templates/vm.bicep --mode incremental --parameters adminPassword=<vm-password> sqlPassword=<sql-password>
```


</details><br/>

VMが作成されたことを確認してください：



```
az vm list -g labs-iaas-bicep -o table
```


セットアップスクリプトはログファイルにログエントリを書き込みます。別のrun-commandを使用してログファイルの出力を表示することができます：


```
az vm run-command invoke  --command-id RunPowerShellScript -g labs-iaas-bicep --scripts "cat /vm-setup.log" -n <vm-name>
```


> アプリが `http://<vm-fqdn>/signup` で正しく動作していることを確認してください。

## ラボ

VM Bicepファイルにはいくつかの問題があります。最初の問題は、デプロイ時に表示される警告ですが、それは私たちにとっては実際の問題ではありませんが、ベストプラクティスに従うべきです。また、テストのためにVMを問い合わせてURLを手動で構築する必要がありました。Bicepファイルを更新してこれらの問題に対処し、もう一度デプロイしてください。セットアップスクリプトは再度実行されますか？

> 詰まった場合は、[ヒント](hints_jp.md)を参照するか、[解決策](solution_jp.md)を確認してください。

___

## クリーンアップ

RGを削除してください：



```
az group delete -y -n labs-iaas-bicep
```
