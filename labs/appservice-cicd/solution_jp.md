# ラボの解決策

ステージングスロットに以下の2つの設定値を設定する必要があります：

- Rng__Range__Min=50
- Rng__Range__Max=500

これは、Portalで_新しいアプリケーション設定_を追加することで行うことができます。

または、CLIで：



```
az webapp config appsettings set --slot staging --settings Rng__Range__Min=50 -g labs-appservice-cicd -n <dns-name>

az webapp config appsettings set --slot staging --settings Rng__Range__Min=500 -g labs-appservice-cicd -n <dns-name>
```


2つのスロットの_設定_ブレードを比較すると、ステージングスロットのみがそれらの設定を持っていることがわかります。

ステージングURLでアプリをテストします：



```
curl https://<dns-name>-staging.azurewebsites.net/rng
```


問題なければ、ステージングとプロダクションのスロットを入れ替えます：



```
az webapp deployment slot swap --slot staging --target-slot production -g labs-appservice-cicd -n <dns-name>
```
