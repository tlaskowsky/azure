# SQL Azure

Azureは、データベースが使用中の場合にのみ支払うサーバーレスオプションから、データセンターのSQL Serverと完全に機能が等しい管理されたVMまで、SQL Server用の複数のサービスを提供しています。すべてのオプションを知る必要がありますが、通常、一つのオプションがほとんどのワークロードに適しています。

## 参考文献

- [Azure SQL ドキュメント](https://docs.microsoft.com/ja-jp/azure/azure-sql/)

- [`az sql server` コマンド](https://docs.microsoft.com/ja-jp/cli/azure/sql/server?view=azure-cli-latest)

- [`az sql db` コマンド](https://docs.microsoft.com/ja-jp/cli/azure/sql/db?view=azure-cli-latest)

## Azure SQL をポータルで探索

ポータルを開いて、新しいAzure SQLリソースを作成するために検索します。サービスの種類は異なります：

- たまにSQLを使用する新しいアプリにはどれを選びますか？
- 仮想マシンオプションが必要な理由は何ですか？

_SQL データベース_ オプションを選択し、_単一データベース_ を作成してください。必要なオプションに注目してください - データベースに取り掛かる前にどのような他のリソースを作成する必要がありますか？

> SQLデータベースはSQL Serverインスタンスに属しており、それはリソースグループに属しています。通常、依存するリソースはポータルで直接作成できます。

データベース用の新しいSQL Serverを作成するリンクに従ってください：

- サーバー名と場所が必要です。任意の名前を使用できますか？
- 認証タイプの選択も必要です。データセンターではWindows認証が好まれますが、ここではデフォルトでSQL認証です。それがクラウドでより適している理由は何でしょうか？

ポータルでデータベースを作成することはありません。代わりにCLIを使用します。

## CLIでSQL Serverを作成

まず、新しいSQLリソースが存在するリソースグループを作成する必要があります。

_グループを作成 - お好きな場所を使用してください：_



```
az group create -n labs-sql --tags courselabs=azure -l southeastasia
```


データベースのホストとなるSQL Serverを作成できます。

> サーバーの名前はグローバルに一意である必要があるため、公開DNS名として使用されます。

📋 `sql server create` コマンドを使用してデータベースサーバーを作成してください。指定する必要があるパラメーターがいくつかあります。

<details>
  <summary>わからない場合</summary>

ヘルプテキストを表示します：



```
az sql server create --help
```


最低限指定する必要があります：

- リソースグループ
- 場所
- サーバー名（グローバルに一意である必要があります）
- 管理者アカウント名
- 管理者パスワード（パスワードポリシーを満たす必要があります）

これで始められます：



```
# ご自身の名前とパスワードを提供する必要があります：
az sql server create -l southeastasia -g labs-sql -n <server-name> -u sqladmin -p <admin-password>
```


</details><br/>

> 新しいSQL Serverの作成には数分かかります。実行中にドキュメントを確認して、次の質問に答えてみてください：

- データベースがないSQL Serverの実行コストはどれくらいですか？

SQL Serverが作成されたら、ポータルにアクセスしてサーバーのプロパティを探します。サーバー名はグローバルに一意である必要があります。

## SQLデータベースを作成

SQL Serverは、ゼロまたはそれ以上のデータベースのコンテナです。SQL Serverがあれば、`sql db create` コマンドを使用してサーバー内に新しいデータベースを作成できます。

📋 `az` コマンドを使用して、SQLサーバー内に `db01` というデータベースを作成してください。

<details>
  <summary>わからない場合</summary>

SQLサーバー名、リソースグループ、データベース名を提供する必要があります：



```
az sql db create -g labs-sql -n db01 -s <server-name>
```

</details><br/>

> これも数分かかります。その間に、ポータルで状況を確認してみてください。それに加えて、以下の質問に答えてみてください:

- 新しいデータベースのデフォルトサイズは何ですか？
- 新しいデータベースに管理者の資格情報を提供する必要がない理由は何ですか？

データベースが作成されると、リモートクライアントから接続できる標準的なSQL Serverインスタンスになります。

## データベースへの接続

SQLデータベースのポータルビューには接続文字列が表示されます。それを使用してSQLクライアントでデータベースに接続します：

- Visual StudioやSQL Server Management Studioがある場合はそれらを使用できます
- または、[VS CodeのSQL Server拡張機能](https://docs.microsoft.com/ja-jp/sql/tools/visual-studio-code/sql-server-develop-use-vscode?view=sql-server-ver15)
- あるいは、[Sqlectron](https://github.com/sqlectron/sqlectron-gui/releases/tag/v1.32.1)のようなシンプルなクライアント ([問題699](https://github.com/sqlectron/sqlectron-gui/issues/699)のため、1.32より新しいバージョンはダウンロードしないでください)

📋 SQL Serverの資格情報を使用して接続を試みてください。データベースにアクセスできますか？

<details>
  <summary>わからない場合</summary>

次のようなエラーが表示されます：

*「sql-labs-03' サーバーにログインを要求されたが、クライアントのIPアドレス '216.213.184.119' からのアクセスは許可されていない。アクセスを有効にするには、Windows Azure管理ポータルを使用するか、マスターデータベースで sp_set_firewall_rule を実行して、このIPアドレスまたはアドレス範囲のファイアウォールルールを作成してください。変更が有効になるまで最大5分かかる場合があります。*

</details><br/>

SQL ServerにはIPブロックがありますので、発信元IPアドレスに基づいてクライアントに明示的にアクセスを許可する必要があります。

ポータルで**SQL Server**インスタンス（データベースではなく）を開き、ファイアウォール設定を見つけます。そのページでは、自分のIPアドレスをルールに簡単に追加できるので、アクセスが許可されます。その後、もう一度接続を試みてください。

## データベースの問い合わせ

接続に成功したら、あなたは管理者の資格情報を使用しているので、DDLおよびDMLステートメントを実行できます：



```
CREATE TABLE students (id INT IDENTITY, email NVARCHAR(150))

INSERT INTO students(email) VALUES ('siddheshpg@azureauthority.in')

SELECT * FROM students
```


> ORM（.NETのEntity FrameworkやJavaのHibernateなど）を使用するアプリケーションでは、空のデータベースを使用できます。設定で接続文字列を設定し、アプリが初めて実行されるときにデータベーススキーマを自動的に設定します。

## ラボ

CLIを使用してSQLデータベースを削除します。データベースがなくなってもSQL Serverは存在しますが、削除されたデータベースからデータを取得できますか？次にリソースグループを削除しますが、SQL Serverはまだ存在しますか？

> 困ったら、[ヒント](hints_jp.md)を試すか、[解決策](solution_jp.md)をチェックしてください。

___

## クリーンアップ

ラボを完了していない場合は、このコマンドでRGを削除して、すべてのリソースを削除できます：



```
az group delete -y -n labs-sql
```
