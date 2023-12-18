# SQL Server VM

管理されたSQL Serverデータベースは多くの管理作業を処理しますが、常にそれらを使用できるわけではありません。データセンターにあるSQL Serverの機能の100%をサポートしているわけではなく、管理されたオプションにない機能が必要な場合があります。

このラボでは、SQL VMサービスを使用します。これにより、基本となるオペレーティングシステムとSQL Serverのデプロイを必要に応じて設定できます。

## 参照

- [Azure SQL Server VM ドキュメント](https://docs.microsoft.com/ja-jp/azure/azure-sql/virtual-machines/?view=azuresql)
- [データベース移行ドキュメント](https://docs.microsoft.com/ja-jp/azure/dms/tutorial-sql-server-to-managed-instance#create-an-azure-database-migration-service-instance)
- [`az sql mi` コマンド](https://docs.microsoft.com/ja-jp/cli/azure/sql/mi?view=azure-cli-latest)

## ポータルでAzure SQLを探索する

ポータルを開き、新しいAzure SQLリソースを作成するために検索します。_SQL Virtual Machines_の詳細を確認します：
- イメージを選択します - LinuxとWindowsのバリアント、異なるSQL ServerバージョンとSKUがあります

ポータルでデータベースを作成する代わりに、CLIを使用します。

## CLIで管理されたSQL Serverを作成する

まず、新しいSQLリソースが存在するリソースグループを作成する必要があります。
_グループを作成します - 自分の好きな場所を使用してください：_



```
az group create -n labs-sql-vm --tags courselabs=azure -l southeastasia
```


次に、使用するVMイメージを見つける必要があります。私たちはWindows Server 2022マシン上のSQL Server 2019 Standardを使用します：



```
# SQL Serverイメージのオファーを探します - これにはWindowsとLinuxが含まれます：
az vm image list-offers --publisher MicrosoftSQLServer -o table

# SKUを探します：
az vm image list-skus -f sql2019-ws2022 -p MicrosoftSQLServer --location southeastasia -o table

# すべてのイメージをリストします、例えば：
az vm image list --sku standard -f sql2019-ws2022 -p MicrosoftSQLServer --location southeastasia -o table --all
```

📋 通常の`vm create`コマンドを使用してSQL Server VMを作成します。

<details>
  <summary>わからない場合</summary>

これで始めることができます - 最新のイメージバージョンを使用してください、URNはこのようになります： _MicrosoftSQLServer:sql2019-ws2022:standard:15.0.220913_



```
az vm create -l southeastasia -g labs-sql-vm -n sql01 --image <urn> --size Standard_D2_v3 --admin-username labs --admin-password <your-strong-password> --public-ip-address-dns-name  <your-dns-name> 
```


</details><br/>

ポータルでVMを開くと、それが特別なSQL Serverオプションのない標準的なVMであることがわかります。

> ネットワークセキュリティグループを確認してください。SQL Serverはポート1433でリスニングしていますが、インターネットからアクセスできますか？

VMにアクセスできたとしても、管理者のユーザー名とパスワードは何でしょうか？通常のVMを作成するときにSQL Server認証を指定することはできません。管理オプションを追加するには、[SQL Server IaaS拡張機能](https://docs.microsoft.com/ja-jp/azure/azure-sql/virtual-machines/linux/sql-server-iaas-agent-extension-linux?view=azuresql&tabs=azure-powershell)にVMを登録する必要があります。

## SQL Server管理のためにVMを登録する

SQL Server拡張機能は、VMをより管理されたデータベースサービスのようなものに変えます。

📋 `sql vm create`コマンドを使用して、VMをSQL Server管理用に登録します。パブリックアクセスを設定し、SQL認証用のユーザー名とパスワードを設定してください。

<details>
  <summary>わからない場合</summary>

ヘルプテキストを表示します：



```
az sql vm create --help
```


指定する必要があるのは：

- VM名 - これは既にSQL Serverが実行されている既存のVMです
- ライセンスタイプ - 企業は既存のSQL Serverライセンスを使用することができます
- 管理タイプ - fullは、すべての管理オプションを提供します

これにより、VMをパブリックアクセス可能なSQL Server VMに変換します：



```
az sql vm create -g labs-sql-vm -n sql01 --license-type PAYG --sql-mgmt-type Full --connectivity-type PUBLIC --sql-auth-update-username labs --sql-auth-update-pwd <strong-password>
```


</details><br/>

> 今、ポータルでVMを開くと、UIはほぼ同じです... しかし、リソースグループを開くと、新しいSQL Virtual Machineリソースがあることがわかります。

ポータルから_Security Configuration_ブレードで接続設定を見ることができます：

- 接続を_Public_に設定
- NSGを確認し、ポート1433への受信トラフィックを許可する新しいルールが追加されていることを確認

## SQL Server VMをカスタマイズする

SQL ServerイメージにはSQL Server Management Studioがプリインストールされているので、ログインしてデータベースを操作するためのUIを使用できます。まず、VMへのRDPアクセスを有効にする必要があります。

📋 インターネットからのポート3389接続を許可するNSGルールを追加してください。

<details>
  <summary>わからない場合</summary>

NSGの名前を見つけます：



```
az network nsg list -g labs-sql-vm  -o table
```


すべての詳細を確認し、RDPルールを追加します：



```
az network nsg rule create -g labs-sql-vm --nsg-name <Your NSG name> -n rdp --priority 150 --source-address-prefixes Internet --destination-port-ranges 3389 --access Allow
```

</details><br/>

これで、VMにログインできます。他のサービスでは利用できないSQL Serverの機能を使用するデモンストレーションを行います - .NETコードを呼び出すカスタム関数を作成します。

- あなたのマシンからVMへ[DLLファイル](/labs/sql-vm/udf/FormattedDate.dll)をコピーします。これはCドライブのルートに配置します。
- （このバイナリファイルには、SQL Serverを通じて利用可能にしたい.NETコードが含まれています。）
- _SQL Server Management Studio_ を実行します。
- デフォルトの接続設定では、マシン名とWindows認証を使用しますが、これで問題ありません。
- 接続して、_新規クエリ_ をクリックし、以下のSQLを実行して、.NETコードを呼び出すUDF（ユーザー定義関数）を登録します。



```
sp_configure 'show advanced options', 1
RECONFIGURE
GO

sp_configure 'clr enabled', 1
RECONFIGURE
GO

sp_configure 'clr strict security', 0
RECONFIGURE
GO

CREATE ASSEMBLY FormattedDate FROM 'C:\FormattedDate.dll';  
GO  
  
CREATE FUNCTION LegacyDate() RETURNS NVARCHAR(7)   
AS EXTERNAL NAME FormattedDate.FormattedDate.LegacyNow;   
GO  
```


> SQL Serverの専門家でなくても心配しないでください :) 

他のAzure SQLオプションでは、ディスクにファイルをアップロードするアクセス権がなく、これらのコマンドの一部が制限されるため、これは実行できません。

さて、UDFをテストしましょう：



```  
SELECT dbo.LegacyDate();  
GO
```


DLLにアップロードした.NETコードによって生成された、レガシーシステム形式の現在の日付が表示されます。

## ラボ

SQL VMのもう一つの使用例として、標準のAzure認証を使用せずに自身で認証を管理し、必要に応じて複数のユーザーを任意のアクセスレベルで作成できます。

新しいSQL Serverログインをユーザー名とパスワードで作成します。それらの資格情報を使用して自分のマシンからデータベースサーバーにアクセスできることを確認し、`SELECT dbo.LegacyDate()` クエリを実行します。

> 困ったら、[ヒント](hints.md)を試すか、[解決策](solution.md)をチェックしてください。

___

## クリーンアップ

次のコマンドでRGを削除し、すべてのリソースを削除します：



```
az group delete -y -n labs-sql-vm
```
