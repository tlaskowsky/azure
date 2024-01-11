# ラボの提案

現在、AzureではSQLの自動化に関して完璧な方法はありません。ローカルのデータベースからBACPACファイルを作成してインポートすることができますが、それには時間がかかることがあります。

別の方法として、ソースコード内にスキーマを保持し、SQL ServerプロジェクトでそれをDACPACにビルドすることができます。その後、SqlPackage（SQL Serverの一部）のようなツールを使用して、デプロイスクリプトを生成することができます（[この例](https://github.com/sixeyed/presentations/blob/master/docker-cambridge/2018-08-ci-cd-database-powered-by-containers/demo3/v1/Initialize-Database.ps1)のように）。
