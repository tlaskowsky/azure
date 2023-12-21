# ラボの解決策

GitHub でリポジトリのフォークを持っていない場合は、[ここ](https://github.com/azureauthority/azure/fork)で作成してください。

自身の API URL を使用するように [index.html](static/index.html) ファイルを更新することを確認してください。それを行う最も簡単な方法は、GitHub で直接編集し、そこで変更をコミットすることです。

次に、フォークから静的 Web アプリをデプロイします：


```
az staticwebapp create  -g labs-appservice-api --branch main --app-location "/labs/appservice-api/static" --login-with-github -l southeastasia -n <dns-name> --source <github-fork-url>
```


アプリのデプロイには数分かかります - GitHub Action のステータスはフォークで確認できます。

完了したら、アプリにアクセスしてランダム数を取得しようとすると、CORS の問題が発生します。

ポータルの API App Service の _CORS_ セクションで _許可されたオリジン_ ドメインを追加できます。

または、CLI で行います：


```
az webapp cors add --allowed-origins 'https://<static-web-fqdn>'  -g labs-appservice-api -n <api-app-service>
```


> ルールが追加されるまでに数分かかります。その後、静的 Web アプリから RNG API を呼び出すことができます：

![SPA RNG アプリ](/img/rng-spa.png)
