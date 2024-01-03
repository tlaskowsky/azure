# Azure コンテナーレジストリ

オープンソースのアプリケーションはしばしば Docker Hub にコンテナーイメージとして公開されます。イメージをホストするサービスをコンテナーレジストリと呼び、自分のアプリケーション用には公開レジストリではなくプライベートレジストリを使用することが望ましいでしょう。Azure コンテナーレジストリは、独自のレジストリを作成し管理するために使用するサービスです。これは Azure のセキュリティと統合されており、コンテナーを実行するサービスと同じリージョンにイメージを保存できます。

## 参考

- [Docker Hub の概要](https://docs.docker.com/docker-hub/)

- [コンテナーレジストリのドキュメント](https://docs.microsoft.com/ja-jp/azure/container-registry/)

- [`az acr` コマンド](https://docs.microsoft.com/ja-jp/cli/azure/acr?view=azure-cli-latest)


## ポータルで ACR を探索する

ポータルを開いて、新しいコンテナーレジストリリソースを作成するために検索します。異なる SKU を通じて切り替え、利用可能なオプションを見てみましょう：

- プライベートネットワーキングと顧客管理の暗号化キーは、プレミアム SKU で利用可能です
- レジストリ名は DNS 名になり、`.azurecr.io` サフィックスが付きますので、全世界でユニークな名前である必要があります。

これで、コマンドラインでレジストリを作成するためにターミナルに戻ります。

## CLI で ACR インスタンスを作成する

ラボ用の新しいリソースグループを、お好みのリージョンで次のように作成します：



```
az group create -n labs-acr --tags courselabs=azure -l eastus
```


📋 `acr create` コマンドで新しいレジストリを作成します。ユニークな名前が必要になります。

<details>
  <summary>方法がわからない場合</summary>

ヘルプから始めます：


```
az acr create --help
```


ポータルでレジストリを作成する場合よりも、ここでのオプションはずっと多いです。管理ページでこれらの多くを設定できます。

これにより、Basic-SKU のレジストリが作成されます：



```
az acr create -g labs-acr -l eastus --sku 'Basic' -n <acr-name>
```


ACR 名には他のものよりも厳しい制限があるため、使用しようとするとエラーが発生するかもしれません。

</details><br/>

コマンドが完了すると、`<acr-name>.azurecr.io` というドメイン名で独自のレジストリが利用可能になります - 出力の `loginServer` フィールドで完全な名前を確認できます。

## ACR へのイメージのプルとプッシュ

Docker イメージ名にはレジストリドメインを含めることができます。デフォルトレジストリは Docker Hub (`docker.io`) なので、それにはドメインが不要です - イメージ `nginx:alpine` の完全な名前は実際には `docker.io/nginx:alpine` です。

イメージをプルすると、最新バージョンがダウンロードされます：



```
docker image pull docker.io/nginx:alpine
```


そのイメージのコピーを ACR にアップロードすることができますが、Docker Hub ではなく ACR ドメインを使用するように名前を変更する必要があります。`tag` コマンドはそれを行います：

_**あなたの** ACR ドメイン名を使用してください：_



```
docker image tag docker.io/nginx:alpine <acr-name>.azurecr.io/labs-acr/nginx:alpine-2204
```


> イメージ名のすべての部分を新しいタグで変更できます。

これで Nginx イメージには2つのタグがあります：



```
docker image ls --filter reference=nginx --filter reference=*/labs-acr/nginx
```


ACR タグと Docker Hub タグはどちらも同じイメージ ID を持っています。タグは別名のようなもので、1つのイメージに多くのタグを付けることができます。

レジストリにイメージをアップロードするには `push` コマンドを使用しますが、まず認証する必要があります。

_ACR にイメージをプッシュしてみてください：_



```
# これは失敗します：
docker image push <acr-name>.azurecr.io/labs-acr/nginx:alpine-2204
```


📋 Azure アカウントでレジストリに認証することができます。`az acr` コマンドでログインしてからイメージをプッシュします。

<details>
  <summary>方法がわからない場合</summary>

ACR コマンドを一覧表示します：



```
az acr --help
```


`login` コマンドがありますが、これには ACR 名だけが必要です：



```
az acr login -n <acr-name>
```


これでイメージをプッシュすると、アップロードされます：



```
docker image push <acr-name>.azurecr.io/labs-acr/nginx:alpine-2204
```


</details><br/>

このコマンドでそのイメージからコンテナを実行できます：



```
docker run -d -p 8080:80 <acr-name>.azurecr.io/labs-acr/nginx:alpine-2204
```


http://localhost:8080 でアプリをブラウズできます。これは標準の Nginx アプリですが、独自のイメージレジストリから利用できます。ACR にアクセスできる人は誰でも同じアプリをイメージから実行できます。

## カスタムイメージのビルドとプッシュ

イメージをビルドするときには、タグにレジストリドメインを含めることができます。次のコマンドを実行して、[Docker 101 ラボ](/labs/docker/README_jp.md)からシンプルな ASP.NET ウェブアプリをビルドし、イメージタグにバージョン番号を含めます：


```
docker build -t  <acr-name>.azurecr.io/labs-acr/simple-web:6.0 src/simple-web
```


> 以前にこのイメージをビルドした場合、Docker は多くのキャッシングを行うため、非常に迅速にビルドされます。

📋 同じイメージに対して、`6.0` ではなく `latest` をバージョン番号として使用して別のタグを作成します。

<details>
  <summary>方法がわからない場合</summary>

`tag` コマンドを使用して、イメージの新しいタグを作成し、名前の任意の部分を変更できます。これにはレジストリドメインまたはバージョン番号が含まれます：


```
docker tag <acr-name>.azurecr.io/labs-acr/simple-web:6.0 <acr-name>.azurecr.io/labs-acr/simple-web:latest
```


</details><br/>

ACR ドメインでタグ付けされたすべてのイメージを一覧表示します：



```
docker image ls <acr-name>.azurecr.io/*/*
```


`simple-web` イメージの2つのバージョンがあります。一つのコマンドで両方のバージョンをプッシュできます：


```
docker push --all-tags <acr-name>.azurecr.io/labs-acr/simple-web
```


## ポータルで ACR をブラウズする

コマンドラインよりもポータルで管理する方が簡単なサービスが ACR です。

ACR にアクセスし、_リポジトリ_ リストを開きます。プッシュしたイメージを確認できます - タグとマニフェストは何を意味していますか？他の ACR 機能も確認してみてください。ウェブフックやレプリケーションは使用するかもしれません。

## ラボ

Azure でコンテナを使用する場合、コード変更のたびにイメージをビルドして ACR にプッシュする CI ジョブがあるかもしれません。ACR ではストレージに対して課金されるため、スケジュールに従って古いイメージをクリーンアップするスクリプトが欲しいかもしれません。

`az` コマンドでイメージを削除する方法を見て、スクリプトを書くのが得意なら、最新の5つのイメージバージョン以外をすべて削除するスクリプトを書いてみてください。

> 詰まったら [ヒント](hints_jp.md) を試してみるか、[解決策](solution_jp.md) を確認してください。

___

## クリーンアップ

このラボのリソースグループを削除すると、ACR インスタンスやそのイメージを含むすべての Azure リソースが削除されます：



```
az group delete -y --no-wait -n labs-acr
```


また、ローカルのすべての Docker コンテナを削除するには、このコマンドを実行します：


```
docker rm -f $(docker ps -aq)
```
