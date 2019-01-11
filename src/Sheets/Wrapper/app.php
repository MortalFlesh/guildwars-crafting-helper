<?php declare(strict_types=1);

require_once __DIR__ . "/vendor/autoload.php";

$sourceFile = __DIR__ . '/../data/update.json';
if (!file_exists($sourceFile)) {
    printfn('Source data file "%s" is missing.', $sourceFile);
    exit(1);
}

[
    'spreadsheetId' => $spreadsheetId,
    'listName' => $listName,
    'data' => $data,
] = json_decode(file_get_contents($sourceFile), true);

function getClient(): Google_Client
{
    $client = new Google_Client();
    $client->setApplicationName('Google Sheets API PHP Quickstart');
    $client->setScopes(Google_Service_Sheets::SPREADSHEETS);
    $client->setAuthConfig(__DIR__ . '/credentials.json');
    $client->setAccessType('offline');
    $client->setPrompt('select_account consent');

    // Load previously authorized token from a file, if it exists.
    // The file token.json stores the user's access and refresh tokens, and is
    // created automatically when the authorization flow completes for the first
    // time.
    $tokenPath = __DIR__ . '/token.json';
    if (file_exists($tokenPath)) {
        $accessToken = json_decode(file_get_contents($tokenPath), true);
        $client->setAccessToken($accessToken);
    }

    // If there is no previous token or it's expired.
    if ($client->isAccessTokenExpired()) {
        // Refresh the token if possible, else fetch a new one.
        if ($client->getRefreshToken()) {
            $client->fetchAccessTokenWithRefreshToken($client->getRefreshToken());
        } else {
            // Request authorization from the user.
            $authUrl = $client->createAuthUrl();
            printf("Open the following link in your browser:\n%s\n", $authUrl);
            print 'Enter verification code: ';
            $authCode = trim(fgets(STDIN));

            // Exchange authorization code for an access token.
            $accessToken = $client->fetchAccessTokenWithAuthCode($authCode);
            $client->setAccessToken($accessToken);

            // Check to see if there was an error.
            if (array_key_exists('error', $accessToken)) {
                throw new Exception(join(', ', $accessToken));
            }
        }
        // Save the token to a file.
        if (!file_exists(dirname($tokenPath))) {
            mkdir(dirname($tokenPath), 0700, true);
        }
        file_put_contents($tokenPath, json_encode($client->getAccessToken()));
    }

    return $client;
}

function rangeOnList(string $listName)
{
    return function (string $range) use ($listName) {
        return sprintf('%s!%s', $listName, $range);
    };
}

function printfn(...$args)
{
    printf(...$args);
    echo "\n";
}

function writeValues(string $spreadsheetId)
{
    return function (string $range, array $values) use ($spreadsheetId) {
        global $service;
        // writing values works as follows
        // if range is a single cell and values are multiple -> values will be written from the starting cell
        // if range is a range of cells, values must be in that range to be written

        $body = new Google_Service_Sheets_ValueRange(['values' => $values]);

        $params = [
            'valueInputOption' => 'USER_ENTERED',
            // this means, it will be inserted same as it would be manually in the browser
        ];

        $service->spreadsheets_values->update($spreadsheetId, $range, $body, $params);
    };
}

// Get the API client and construct the service object.
$client = getClient();
$service = new Google_Service_Sheets($client);

$rangeOnList = rangeOnList($listName);
$write = writeValues($spreadsheetId);

//$response = $service->spreadsheets_values->get($spreadsheetId, $rangeOnList('A1'));
//$values = $response->getValues();
//
//if (empty($values)) {
//    printfn('No values to write');
//}

//$name = $values[0][0];
//printfn("App for %s", $name);
//printfn(str_repeat('=', mb_strlen($name)));

foreach ($data as $range => $values) {
    $write($rangeOnList($range), $values);
}

printfn('Done');
