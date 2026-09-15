<?php
// Source: ecomwebdev/skyrise/src/Environment.php (complete copy)
// IMPORTANT: the DEVELOPMENT host here (skyrise-qa.webrootcloudav.com) is what SkyRise\Client
// actually uses -- NOT connections.dev.json's 'skyrise.host' value (skyrisesvc-qa...), which is
// shown commented-out below as superseded. See doc 04, Section 3.8.

namespace SkyRise;

class Environment {

    /**
     * Define SkyRise development envrionment
     *
     * @const
     * @var string
     */
    const DEVELOPMENT = 'dev';

    /**
     * Define SkyRise pre-production environment.
     * This is used by 3rd parties needing to test their integrations outside
     * of the Webroot internal network.
     *
     * @const
     * @var string
     */
    const PRE_PRODUCTION = 'pre';

    /**
     * Define SkyRise production envrionment
     *
     * @const
     * @var string
     */
    const PRODUCTION  = 'prod';

    /**
     * All valid SkyRise envrionments
     *
     * @const
     * @var array
     */
    const NAMES = [
        self::DEVELOPMENT,
        self::PRE_PRODUCTION,
        self::PRODUCTION
    ];

    /**
     * SkyRise hosts, indexed by environment
     *
     * @const
     * @var array
     */
    const HOSTS = [
        // self::DEVELOPMENT    => 'https://skyrise-pre.webrootcloudav.com/',
        // self::DEVELOPMENT    => 'https://skyrisesvc-qa.webrootcloudav.com',
        self::DEVELOPMENT    => 'https://skyrise-qa.webrootcloudav.com',
        self::PRE_PRODUCTION => 'https://skyrise-pre.webrootcloudav.com',
        self::PRODUCTION     => 'https://skyrisesvc.webrootcloudav.com'
    ];

    // NOTE: full remainder of this class (constructor, getHost(), isProduction(), etc.)
    // was not captured in this reference copy -- pull the complete file from
    // ecomwebdev/skyrise/src/Environment.php in the main workspace if you need it verbatim.
    // The HOSTS/NAMES constants above are the load-bearing part for this service.
}
