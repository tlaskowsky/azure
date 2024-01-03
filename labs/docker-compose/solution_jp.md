# ラボの解答

サンプル解答 [lab/compose.yaml](./lab/compose.yml) には以下が追加されています:

- Dockerのデフォルトで作成される、オプションなしの `front-end` というネットワーク
- Nginxイメージを使用し、`front-end` と `app-net` ネットワークに接続する `nginx` というサービス。

ComposeはYAMLファイルのディレクトリ名を使用してアプリケーションを識別するため、アプリを更新するにはComposeファイルを`rng`フォルダにコピーする必要があります。

Composeファイルをコピーして更新をデプロイします：



```
cp ./labs/docker-compose/lab/compose.yml ./labs/docker-compose/rng/lab.yml

docker-compose -f ./labs/docker-compose/rng/lab.yml up -d
```


> 新しいネットワークとコンテナが作成されますが、RNG WebとAPIコンテナは変更されません。これらのサービスの仕様は変更されていないので、コンテナは望ましい状態と一致しています。

新しいコンテナを検査してネットワークの詳細を表示します：



```
docker inspect rng-nginx-1
```


> ネットワークセクションの最後にある出力で、それぞれのネットワークから1つずつ、合計2つのIPアドレスが見られます。これは、2枚のネットワークカードを持つマシンのようなものです。

NginxコンテナからWebコンテナへの接続性をテストします：



```
docker exec rng-nginx-1 nslookup rng-web
```


> 新しいコンテナは元のコンテナのIPアドレスを解決できます。

そしてWebコンテナからNginxへ：



```
docker exec rng-rng-web-1 ping -c3 nginx
```


> 古いコンテナは新しいコンテナに到達できます。

> [演習](README_jp.md)に戻る。
