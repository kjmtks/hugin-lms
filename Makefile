APPNAME="Hugin"

KEY=""
CER=""

PRODUCTION_OVERRIDE_FILE=docker-compose.production.override.yml
LOCAL_OVERRIDE_FILE=docker-compose.local.override.yml

production-up:
ifeq (${PRODUCTION_OVERRIDE_FILE}, $(shell ls | grep ${PRODUCTION_OVERRIDE_FILE}))
	docker-compose -f docker-compose.production.yml -f ${PRODUCTION_OVERRIDE_FILE} build
	docker-compose -f docker-compose.production.yml -f ${PRODUCTION_OVERRIDE_FILE} up -d
else
	docker-compose -f docker-compose.production.yml build
	docker-compose -f docker-compose.production.yml up -d
endif

production-logs:
ifeq (${PRODUCTION_OVERRIDE_FILE}, $(shell ls | grep ${PRODUCTION_OVERRIDE_FILE}))
	docker-compose -f docker-compose.production.yml -f ${PRODUCTION_OVERRIDE_FILE} logs
else
	docker-compose -f docker-compose.production.yml logs
endif

production-bash:
ifeq (${PRODUCTION_OVERRIDE_FILE}, $(shell ls | grep ${PRODUCTION_OVERRIDE_FILE}))
	docker-compose -f docker-compose.production.yml -f ${PRODUCTION_OVERRIDE_FILE} run hugin /bin/bash
else
	docker-compose -f docker-compose.production.yml run hugin /bin/bash
endif

production-up-debug:
ifeq (${PRODUCTION_OVERRIDE_FILE}, $(shell ls | grep ${PRODUCTION_OVERRIDE_FILE}))
	docker-compose -f docker-compose.production.yml -f ${PRODUCTION_OVERRIDE_FILE} build
	docker-compose -f docker-compose.production.yml -f ${PRODUCTION_OVERRIDE_FILE} up
else
	docker-compose -f docker-compose.production.yml build
	docker-compose -f docker-compose.production.yml up
endif

production-down:
ifeq (${PRODUCTION_OVERRIDE_FILE}, $(shell ls | grep ${PRODUCTION_OVERRIDE_FILE}))
	docker-compose -f docker-compose.production.yml -f ${PRODUCTION_OVERRIDE_FILE} down
	docker-compose -f docker-compose.production.yml -f ${PRODUCTION_OVERRIDE_FILE} down
else
	docker-compose -f docker-compose.production.yml down
	docker-compose -f docker-compose.production.yml down
endif


production-remove:
ifeq (${PRODUCTION_OVERRIDE_FILE}, $(shell ls | grep ${PRODUCTION_OVERRIDE_FILE}))
	docker-compose -f docker-compose.production.yml -f ${PRODUCTION_OVERRIDE_FILE} down
	docker-compose -f docker-compose.production.yml -f ${PRODUCTION_OVERRIDE_FILE} down
else
	docker-compose -f docker-compose.production.yml down
	docker-compose -f docker-compose.production.yml down
endif
	docker volume rm lms7_db-data lms7_app-data



local-up:
ifeq (${LOCAL_OVERRIDE_FILE}, $(shell ls | grep ${LOCAL_OVERRIDE_FILE}))
	docker-compose -f docker-compose.local.yml -f ${LOCAL_OVERRIDE_FILE} build
	docker-compose -f docker-compose.local.yml -f ${LOCAL_OVERRIDE_FILE} up -d
else
	docker-compose -f docker-compose.local.yml build
	docker-compose -f docker-compose.local.yml up -d
endif

local-logs:
ifeq (${LOCAL_OVERRIDE_FILE}, $(shell ls | grep ${LOCAL_OVERRIDE_FILE}))
	docker-compose -f docker-compose.local.yml -f ${LOCAL_OVERRIDE_FILE} logs
else
	docker-compose -f docker-compose.local.yml logs
endif

local-bash:
ifeq (${LOCAL_OVERRIDE_FILE}, $(shell ls | grep ${LOCAL_OVERRIDE_FILE}))
	docker-compose -f docker-compose.local.yml -f ${LOCAL_OVERRIDE_FILE} run hugin /bin/bash
else
	docker-compose -f docker-compose.local.yml run hugin /bin/bash
endif

local-up-debug:
ifeq (${LOCAL_OVERRIDE_FILE}, $(shell ls | grep ${LOCAL_OVERRIDE_FILE}))
	docker-compose -f docker-compose.local.yml -f ${LOCAL_OVERRIDE_FILE} build
	docker-compose -f docker-compose.local.yml -f ${LOCAL_OVERRIDE_FILE} up
else
	docker-compose -f docker-compose.local.yml build
	docker-compose -f docker-compose.local.yml up
endif

local-down:
ifeq (${LOCAL_OVERRIDE_FILE}, $(shell ls | grep ${LOCAL_OVERRIDE_FILE}))
	docker-compose -f docker-compose.local.yml -f ${LOCAL_OVERRIDE_FILE} down
	docker-compose -f docker-compose.local.yml -f ${LOCAL_OVERRIDE_FILE} down
else
	docker-compose -f docker-compose.local.yml down
	docker-compose -f docker-compose.local.yml down
endif


local-remove:
ifeq (${LOCAL_OVERRIDE_FILE}, $(shell ls | grep ${LOCAL_OVERRIDE_FILE}))
	docker-compose -f docker-compose.local.yml -f ${LOCAL_OVERRIDE_FILE} down
	docker-compose -f docker-compose.local.yml -f ${LOCAL_OVERRIDE_FILE} down
else
	docker-compose -f docker-compose.local.yml down
	docker-compose -f docker-compose.local.yml down
endif
	docker volume rm lms7_db-data lms7_app-data





pfx:
	openssl pkcs12 -export -out ./keys/server.pfx -inkey ${KEY} -in ${CER}

clean:
	rm -rf ${APPNAME}/bin
	rm -rf ${APPNAME}/obj
	rm -rf ${APPNAME}/etc
	rm -rf ${APPNAME}/node_modules
	rm -rf ${APPNAME}/out/wwwroot
