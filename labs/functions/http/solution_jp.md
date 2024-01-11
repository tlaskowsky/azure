# ラボ 解決策

ラボ用の新しいディレクトリを作成します：


```
mkdir ../lab 

cd ../lab
```


`func new` を実行し、プロンプトに従います：

- PowerShellを選択
- HTTPトリガーを選択
- トリガーに名前を付ける（例："hello"）

関数を開始します：



```
func start
```


PowerShellセッションで実行している場合、関数は実行され、試すことができます：


```
curl http://localhost:7071/api/hello?name=courselabs
```


PowerShellがインストールされていない場合、関数はローカルで実行に失敗します。インストールされているランタイムを選択する必要があります。

しかし、Azureにデプロイすることはまだ可能です。

同じFunctionアプリを使用してみてください：



```
func azure functionapp publish <function-name>
```


> これは失敗します。なぜなら、1つのFunctionアプリ内のすべての関数は同じ言語ランタイムを使用する必要があるため、既存の関数は.NETです

消費モデルプランのホスティングプランは単なるプレースホルダーであり、サーバーやコストはありません。同じリージョンで新しいFunctionアプリを作成することができ、それは同じプランを使用します：


```
az functionapp create -g labs-functions-http  --runtime powershell --functions-version 4 --consumption-plan-location eastus --storage-account <sa-name> -n <function-name-2> 
```


これで公開できます：



```
func azure functionapp publish <function-name-2>
```


> 今回はコンパイルステップがありません。これはスクリプト関数だからです

これは公開され、問題なく実行されるはずです。ポータルで確認してください - 関数はデフォルトで認証が必要であり、_関数URLを取得_ ボタンでキーを取得できます。
