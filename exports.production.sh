export LANG=ja_JP.UTF-8
export TZ=Asia/Tokyo
export APP_URL="https://hugin-lms.net"
export POSTGRES_DB=hugin-production
export POSTGRES_USER=hugin
export POSTGRES_PASSWORD=password
export APP_SECRET_KEY="your-secret-key(need-32-characters)"
export APP_DATA_PATH="$HOME/hugin-app-data"
export APP_NAME=Hugin
export APP_DESCRIPTION=""      
export ASPNETCORE_ENVIRONMENT=Production
export ASPNETCORE_URLS="https://+:443"
export ASPNETCORE_Kestrel__Certificates__Default__Path="/home/kojima/keys/server.pfx"
export ASPNETCORE_Kestrel__Certificates__Default__Password="password"
export DB_CONNECTION_STR="Host=localhost; Database=$POSTGRES_DB; Port=5432; Username=$POSTGRES_USER; Password=$POSTGRES_PASSWORD"
export PATH_TO_PROTECTION_KEY="$HOME/hugin-data-protection"

### for LDAP
# export LDAP_HOST="ldap.****.**"
# export LDAP_PORT="636"
# export LDAP_BASE="dc=***,dc=***,..."
# export LDAP_ID_ATTR="uid"
# export LDAP_MAIL_ATTR="mail"
# export LDAP_NAME_ATTR="displayName;lang-ja"
# export LDAP_ENGNAME_ATTR="displayName"
