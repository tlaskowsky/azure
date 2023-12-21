# App Service の CI/CD

Azure App Service の2つの機能、_デプロイメント_ と _デプロイメントスロット_ によって、ソースコードが変更されて Git リポジトリにプッシュされるたびにトリガーされる継続的インテグレーションと継続的デプロイメントがサポートされています。

このラボでは、自分の GitHub リポジトリからランダムナンバーAPIをデプロイし、変更をプッシュすることで CI/CD を設定します。

## 参考資料

- [GitHub からの App Service 連続デプロイメント](https://docs.microsoft.com/en-us/azure/app-service/scripts/cli-continuous-deployment-github)
- [App Service のステージング環境](https://learn.microsoft.com/en-us/azure/app-service/deploy-staging-slots)
- [`az webapp deployment` コマンド](https://learn.microsoft.com/en-us/cli/azure/webapp/deployment?view=azure-cli-latest)

## GitHub からの App Service デプロイメント

以前の[静的 Web アプリのラボ](/labs/appservice-static)で説明したように、GitHub に自分のフォークがあるはずです。なければ、無料の GitHub アカウントに[サインアップ](https://github.com/signup)してフォークを[作成](https://github.com/azureauthority/azure/fork)できます。

その後、フォークをリモートとして追加します：


```
git remote add fork <github-fork-url>
```


> 既にフォークがあるが同期されていない場合は、削除して新しいものを作成することができます。

📋 リソースグループと Linux App Service プラン（ワーカー2台）を作成します。CI/CD 機能を利用するには、Standard SKU を使用する必要があります。

<details>
  <summary>分からない場合はこちら</summary>

特に新しいことはありません：



```
az group create -n labs-appservice-cicd --tags courselabs=azure

az appservice plan create -g labs-appservice-cicd -n app-plan-01 --is-linux --sku S1 --number-of-workers 2
```


</details><br/>

GitHub でフォークの URL を見つけます。[azureauthority/azure](https://github.com/azureauthority/azure) を開き、_Forks_ ボタンを展開して自分のフォークへのリンクを見つけます：

![GitHub のフォークリンク](/img/github-fork-link.png)

> 私のものは https://github.com/siddheshp/azure ですが、あなたのものは GitHub のユーザー名を含んだ URL になります。

Web アプリを作成し、デプロイメント設定を構成します：



```
# アプリを作成：
az webapp create -g labs-appservice-cicd --plan app-plan-01 --runtime dotnetcore:6.0 -n <dns-name>

# デプロイメントのパスを設定：
az webapp config appsettings set --settings PROJECT='src/rng/Numbers.Api/Numbers.Api.csproj' -g labs-appservice-cicd -n <dns-name>
```


> `webapp up` を使用すると、ローカルマシン上のコードからデプロイされますが、代わりに Web アプリを作成し、設定でプロジェクトのパスを設定しました。

アプリはまだデプロイされていません - App Service の URL にアクセスすると、デプロイ待ちのページが表示されます。

最初の手動デプロイメントを作成します - GitHub リポジトリはパブリックなので、Azure は認証なしでフェッチできます：



```
# リポジトリの URL に .git サフィックスを追加してください：
az webapp deployment source config -g labs-appservice-cicd --manual-integration --branch main -n <dns-name> --repo-url <github-fork-url>.git
```


Portal で App Service を開き、_Deployment center_ タブでステータスを確認します。_Settings_ タブでは GitHub の設定が、_Logs_ タブでは現在のデプロイメントの状態が表示されます。

最初のデプロイメントには数分かかります。完了したら、curl を使って API が動作していることを確認します：



```
curl https://<app-fqdn>/rng
```


> このデプロイメントは失敗しないように設定されているので、新しい数値を得るために呼び出しを続けることができます。

[src/rng/Numbers.Api/appsettings.json](/src/rng/Numbers.Api/appsettings.json) の設定を更新して、ランダム数値の範囲を変更します。

- 最小値を 1000 に設定
- 最大値を 10000 に設定

**GitHub で直接行うことができます**。ファイルを開き、編集して変更を保存します。または、ローカルマシンで変更し、コミットしてプッシュします：


``` 
git add src/rng/Numbers.Api/appsettings.json

git commit -m 'Change RNG range'

git push labs-appservice-cicd
```


Portal の _Deployment Center_ を確認すると、ソースリポジトリが変更されても新しいデプロイメントが行われていないことがわかります。現在は _manual integration_ を使用しているため、デプロイメントを手動で開始する必要があります。_Sync_ をクリックして、最新のコードに更新します。

デプロイメントが完了すると、curl コマンドを繰り返すと、ランダム数値が大きくなります：



```
curl https://<app-fqdn>/rng
```


## CI/CD の設定

継続的インテグレーション (CI) はビルドの手動トリガーを排除します - GitHub への変更がプッシュされるたびに新しいデプロイメントがトリガーされます。次に CI に切り替えます。

Azure が GitHub と CI で連携するためには、認証を設定する必要があります。Azure は GitHub トークン（Personal Access Token、PAT）を使用して認証します：

- https://github.com/settings/tokens/new にアクセスします
- MFA を使用している場合は、再度サインインする必要があります
- メモを入力して、PAT の用途を覚えておきます
- `workflow` と `admin:repo_hook` の権限を選択します
- _Generate Token_ をクリックしてトークンをコピーします（これが唯一のチャンスです）

> トークンは `ghp_` で始まるランダムな文字列になります。例：`ghp_asd3YWHHefefgd2vZgege878AAH`

手動デプロイメントソースを削除し、GitHub トークンを使用した継続的デプロイメントに置き換えます：


```
az webapp deployment source delete -g labs-appservice-cicd -n <dns-name>

az webapp deployment source config -g labs-appservice-cicd --branch main -n <dns-name> --repo-url <github-fork-url>.git --git-token <github-token>
```

> コマンドが時々スタックすることがあります - 数分以内に返ってこない場合は、Ctrl-Cでキャンセルし、同じコマンドを再度実行してください。

_Deployment Center_ の _Settings_ タブを確認すると、GitHubのユーザー名が表示されます。デプロイメントが完了すると、最新のコミットを使用して、これはより大きなランダム数値範囲用です。

再度設定を変更し、GitHubにプッシュすると、自動的にデプロイメントがトリガーされます。


## ステージング デプロイメント スロットの追加

デプロイメントスロットを使用すると、CI/CDが新しいリリースを一時的な（"ステージング"）環境に公開でき、本番環境に新バージョンを送る前にテストすることができます。

`staging` という名前のスロットを作成し、そのスロットのデプロイメントを設定します：



```
az webapp deployment slot create --slot staging -g labs-appservice-cicd -n <dns-name>

az webapp config appsettings set --slot staging --settings PROJECT='src/rng/Numbers.Api/Numbers.Api.csproj' -g labs-appservice-cicd -n <dns-name>
```


デプロイメントスロットは通常、ソースコードのブランチに対応しているため、`staging` をプッシュするとステージングデプロイメントがトリガーされます。

- GitHubのフォークリポジトリにアクセスし、_branches_ ボタンから `staging` という新しいブランチを作成します

次に、ステージングスロットがステージングブランチからデプロイされるようにデプロイメントソースを追加します：


```
az webapp deployment source config -g labs-appservice-cicd --branch staging --slot staging -n <dns-name> --repo-url <github-fork-url>.git --git-token <github-token>
```


> コマンドがスタックすることがあるので、数分以内に返ってこない場合は、Ctrl-Cでキャンセルし、同じコマンドを再度実行してください。

Portalで _Deployment slots_ をチェックし、ステージングスロットに切り替えます。独自のURLがあります。

デプロイメントが完了したら、両方のスロットからのレスポンスを確認できます：



```
# これは本番環境です：
curl https://<dns-name>.azurewebsites.net/rng

# これはステージングです：
curl https://<dns-name>-staging.azurewebsites.net/rng
```


ステージングスロットに新バージョンがあり、本番環境に移行する準備ができていれば、スロットをスワップし、ステージングスロットが本番スロットになります。

## ラボ

ステージングスロットのアプリ設定を更新して、ランダム数値の範囲を 50-500 に設定します。変更をテストし、満足したらスロットをスワップして、本番環境が新しい範囲を使用するようにします。

> 分からない場合は、[ヒント](hints_jp.md)を試すか、[解決策](solution_jp.md)を確認してください。

___

## クリーンアップ

リソースグループを削除してクリーンアップします：



```
az group delete -y -n labs-appservice
```
