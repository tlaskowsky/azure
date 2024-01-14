# 関数のための継続的デプロイメント

`func` コマンドラインはローカル開発や素早いプロトタイプには適していますが、チーム配信には集中化されたSCMに基づく管理されたプロセスが必要です。Functionアプリは他のApp Serviceアプリと同様に、GitリポジトリからCI/CDを設定できます。

このラボでは、GitHub Actionsを使用したCI/CDで一連のチェーン化された関数をデプロイし、いくつかの関数更新でワークフローをテストします。

## 参考文献

- [GitHub Actionsを使用したFunctionsの継続的デリバリ](https://learn.microsoft.com/en-us/azure/azure-functions/functions-how-to-github-actions?tabs=dotnet)

## GitHubからのFunction Appデプロイメント

GitHubでラボリポジトリの自分のフォークを持っている必要があります（これは[静的Webアプリラボ](/labs/appservice-static/README.md)でカバーしました）。持っていない場合は、無料のGitHubアカウントにサインアップし、[フォークを作成](https://github.com/courselabs/azure/fork)することができます。

> 既にフォークを持っている場合は、GitHubで_Sync Fork_をクリックして更新するか、削除して別のものを作成できます。

Azureで直ちに始めましょう。

📋 消費プランを使用してリソースグループとFunctionアプリを作成します。Functionアプリにはストレージアカウントが必要であることを覚えておいてください。

<details>
  <summary>方法がわからない場合</summary>

ここでは新しいことはほとんどありません：



```
az group create -n labs-functions-cicd --tags courselabs=azure -l eastus

az storage account create -g labs-functions-cicd --sku Standard_LRS -l eastus -n <sa-name>

az functionapp create -g labs-functions-cicd  --runtime dotnet --functions-version 4 --consumption-plan-location eastus --storage-account <sa-name> -n <fn-name>
```


</details><br/>

FunctionアプリはGitHub Actionsを使用してデプロイできます。Azure CLIはGitHubリポジトリに接続し、認証を求め、その後パイプラインのYAMLファイルを作成することができます。

GitHubデプロイメントを使用してFunctionアプリを作成すると、端末に表示されたコードを求めてブラウザが開きます：


```
az functionapp deployment github-actions add --branch main --build-path 'labs/functions/cicd/ChainedFunctions' --runtime-version 6 --login-with-github -g labs-functions-cicd --repo '<your-github-fork>' -n <fn-name> 
```

> 完了したら、GitHubフォークのワークフローファイルを確認します。

GitHub Actionsのワークフローはリポジトリの`.github`フォルダにYAMLファイルで保存されます。GitHubでファイルを閲覧し、内容を確認してください。Functionアプリ名が正しく設定されていない可能性があります。

以下のように見える場合：


```
env:
  AZURE_FUNCTIONAPP_NAME: 'your-app-name'   # set this to your function app name on Azure
```

GitHubで編集アイコン（ペンのような見た目）をクリックして、実際のFunctionアプリ名に名前を変更します：


```
env:
  AZURE_FUNCTIONAPP_NAME: '<fn-name>'
```


緑色の_Start Commit_ボタンをクリックし、変更をコミットします。これにより新しいビルドがトリガーされます。

## Functionの設定

_Actions_ビューに移動すると、アプリのビルドとデプロイが表示されます。

Azureポータルに戻ると、`Heartbeat`関数がリストされているのが見えます（コードは[Scheduled Functionsラボ](/labs/functions/timer/README.md)に似ています）：

- ポータルで実行されるコードを確認できますか？
- バインディングはポータルにリストされていますが、スケジュールを編集できますか？
- Functionの実行を待ちますが、なぜ失敗するのでしょうか？

📋 Functionが必要とする依存関係を作成し、Functionアプリ内の不足している設定を構成します。

<details>
  <summary>方法がわからない場合</summary>

Functionは別のストレージアカウントにblobを書き込むことを期待しています。それを作成し、Functionのアプリ設定に接続文字列を設定する必要があります。

Functionのバインディングビューは必要な設定名を教えてくれます。

</details><br/>

次回のfunctionの実行で、すべてが正しく動作し、Storageアカウントにblobが作成されていることを確認します。

## より多くのFunctionsの追加

プロジェクトに追加できる他のfunctionsがいくつかあります。これらは一連のチェーン化されたワークフローを表しています - 一つのfunctionの出力が次のトリガーとして機能します：

- [ChainedFunctions/Heartbeat.cs](/labs/functions/cicd/ChainedFunctions/Heartbeat.cs) - タイマーによってトリガーされ、blobストレージに書き込む起源のfunction
- [update/WriteLog.cs](/labs/functions/cicd/update/WriteLog.cs) - blob作成によってトリガーされ、Table Storageにエンティティを書き込みます
- [update/NotifySubscribers.cs](/labs/functions/cicd/update/NotifySubscribers.cs) - またblob作成によってトリガーされ、Azure Service Busにメッセージを公開します

メインプロジェクトにこれらを追加し、変更をプッシュすることで、ビルドがトリガーされ、デプロイされます：


```
# フォークのためのリモートを追加：
git remote add labs-funcions-cicd <your-github-fork-url>

# ワークフローをダウンロードするために変更をプル：
git pull labs-funcions-cicd

# プロジェクトフォルダに新しいfunctionsをコピー：
cp labs/functions/cicd/update/*.cs labs/functions/cicd/ChainedFunctions/

# 変更をコミットしてプッシュ：
git add labs/functions/cicd/ChainedFunctions/*
git commit -m 'New functions'
git push labs-funcions-cicd main
```


GitHubでビルドの進行を監視し、その後新しいfunctionsが実行されていることを確認します。**一つは失敗します**

📋 Azureでさらに一つの依存関係を作成し、新しいfunctionsの一つの設定を構成する必要があります。

<details>
  <summary>方法がわからない場合</summary>

新しいFunctionはService Bus Queueにメッセージを書き込みます - そのためには、namespaceとqueueを作成し、接続文字列をFunctionアプリ設定として設定する必要があります。

Functionのバインディングビューは必要な設定名を教えてくれます。

</details><br/>

すべてが正常に動作すると、タイマートリガーが最終的に3つの出力 - blob、Table Storageエンティティ、Service Busメッセージ - をもたらすことがわかります。

## ラボ

ここではfunctionsのチェーンに関するいくつかの質問があります。Table Storageの書き込みからService Bus通知がトリガーされないのはなぜでしょうか？その場合、functionsは既知の順序で実行されることが保証されます。そして、実際の目的がTable Storageエンティティとメッセージである場合、blobを作成せずにこれを再構成できますか？

> 詰まった場合は、[提案](suggestions_jp.md)を試してみてください。

___

## クリーンアップ

ラボRGを削除します：



```
az group delete -y --no-wait -n labs-functions-cicd
```
