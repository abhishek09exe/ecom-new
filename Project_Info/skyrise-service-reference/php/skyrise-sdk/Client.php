<?php
// Source: ecomwebdev/skyrise/src/Client.php (complete copy)
// Base HTTP client -- host resolution comes from SkyRise\Environment (see Environment.php in this
// same folder), NOT from etc/connections.dev.json's 'skyrise.host' field. See doc 04, Section 3.8.

namespace SkyRise;

use Psr\Http\Message\ResponseInterface;
use GuzzleHttp\Client as GuzzleHttpClient;
use GuzzleHttp\Promise;
use GuzzleHttp\Promise\PromiseInterface;

class Client {

    /**
     * The environment object, contains which host to use
     *
     * @private
     * @var SkyRise\Environment
     */
    private $_env;

    /**
     * Constructor, takes the environment name to use.
     *
     * @public
     * @param string $envName The environment name, compatible with SkyRise\Environment
     */
    public function __construct(string $envName) {
        $this->_env = new Environment($envName);
    }

    /**
     * Get the host, including the scheme (e.g. https://www.webroot.com)
     *
     * @public
     * @return string
     */
    public function getHost(): string {
        return $this->_env->getHost();
    }

    /**
     * Tests whether this client is configured for production
     *
     * @public
     * @return bool
     */
    public function isProduction(): bool {
        return $this->_env->isProduction();
    }

    /**
     * Send a single SkyRise request.
     *
     * @public
     * @param \SkyRise\Request
     */
    public function sendRequest(Request $req): ResponseInterface {
        $responses = $this->sendRequestBatch([ $req ]);
        return $responses[0];
    }

    /**
     * Send a batch of requests for a single SkyRise call.
     *
     * @public
     * @param array An array of \SkyRise\Request
     * @return array Array of \Psr\Http\Message\ResponseInterface
     */
    public function sendRequestBatch(array $requests): array {
        $guzzleClient = $this->_createGuzzleHttpClient();
        $promises     = [];
        foreach ($requests as $i => $req) {
            $promises[$i] = $guzzleClient->sendAsync($req->createPsr7Request());
        }
        $responses = self::_collectResponses($promises);
        return $responses;
    }

    /**
     * Create a Guzzle HTTP Client, used to send concurrent/async requests.
     *
     * @private
     * @return \GuzzleHttp\Client
     */
    private function _createGuzzleHttpClient(): GuzzleHttpClient {
        return new GuzzleHttpClient([
            'base_uri' => $this->getHost()
        ]);
    }

    /**
     * Loop over resolved promises and collect the responses into an
     * array, preserving indices.
     *
     * @private
     * @static
     * @param array $promises
     * @return array
     */
    private static function _collectResponses(array $promises): array {
        $results   = Promise\settle($promises)->wait();
        $responses = [];
        foreach ($results as $i => $promise) {
            $response = null;
            switch ($promise['state']) {
                case PromiseInterface::FULFILLED:
                    $response = $promise['value'];
                    break;
                case PromiseInterface::REJECTED:
                    $response = $promise['reason']->getResponse();
                    break;
            }
            $responses[$i] = $response;
        }
        return $responses;
    }
}
