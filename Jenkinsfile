pipeline {
    agent any

    options {
        skipDefaultCheckout()
        disableConcurrentBuilds()
        timeout(time: 20, unit: 'MINUTES')
    }

    triggers {
        pollSCM('H/2 * * * *')
    }

    environment {
        POSTGRES_PASSWORD = credentials('taskboard-db-password')
    }

    stages {
        stage('Checkout') {
            steps {
                checkout scm
            }
        }

        stage('Check tools and configuration') {
            steps {
                sh '''
                    set -eu
                    docker info > /dev/null
                    docker compose version
                    python3 --version
                    curl --version
                    docker compose -p taskboard config --quiet
                '''
            }
        }

        stage('Build images') {
            steps {
                sh '''
                    set -eu
                    docker compose -p taskboard build --pull api
                    docker compose -p taskboard build --pull web
                '''
            }
        }

        stage('Deploy') {
            steps {
                sh '''
                    set -eu
                    docker compose -p taskboard up -d --no-build
                '''
            }
        }

        stage('Verify deployment') {
            steps {
                sh '''
                    set -eu
                    curl --retry 12 --retry-delay 5 \
                        --retry-connrefused --max-time 10 \
                        -fsS http://localhost/health

                    BASE_URL=http://localhost \
                        python3 scripts/smoke-test.py

                    docker compose -p taskboard ps
                '''
            }
        }
    }
}
