<?php
// Source: ecomwebdev/skyidentity/src/Environment.php (complete copy)

namespace SkyIdentity;

class Environment {

    /**
     * Define SkyIdentity development envrionment
     *
     * @const
     * @var string
     */
    const DEVELOPMENT = 'dev';

    /**
     * Define SkyIdentity production envrionment
     *
     * @const
     * @var string
     */
    const PRODUCTION  = 'prod';

    /**
     * All valid SkyIdentity envrionments
     *
     * @const
     * @var array
     */
    const NAMES = [
        self::DEVELOPMENT,
        self::PRODUCTION
    ];

    /**
     * SkyIdentity hosts, indexed by environment
     *
     * @const
     * @var array
     */
    const HOSTS = [
        self::DEVELOPMENT    => 'https://skyidentity-qa.webrootcloudav.com',
        self::PRODUCTION     => 'https://skyidentity.webrootcloudav.com'
    ];

    // NOTE: remainder of this class (constructor, getHost(), isProduction()) not captured
    // in this reference copy -- same pattern as SkyRise\Environment. Pull the complete file
    // from ecomwebdev/skyidentity/src/Environment.php if needed verbatim.
}
