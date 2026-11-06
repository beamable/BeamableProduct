/**
 * ⚠️ THIS FILE IS AUTO-GENERATED. DO NOT EDIT MANUALLY.
 * All manual edits will be lost when this file is regenerated.
 */

import { GET } from '@/constants';
import { makeApiRequest } from '@/utils/makeApiRequest';
import { POST } from '@/constants';
import type { CompleteGitHubSignInRequest } from '@/__generated__/schemas/CompleteGitHubSignInRequest';
import type { GitHubSignInInfoResponse } from '@/__generated__/schemas/GitHubSignInInfoResponse';
import type { HttpRequester } from '@/network/http/types/HttpRequester';
import type { HttpResponse } from '@/network/http/types/HttpResponse';
import type { ScmHostTokenResponse } from '@/__generated__/schemas/ScmHostTokenResponse';
import type { StartGitHubSignInRequest } from '@/__generated__/schemas/StartGitHubSignInRequest';
import type { StartGitHubSignInResponse } from '@/__generated__/schemas/StartGitHubSignInResponse';

/**
 * @remarks
 * **Authentication:**
 * This method requires a valid bearer token in the `Authorization` header.
 * 
 * @param requester - The `HttpRequester` type to use for the API request.
 * @param gamertag - Override the playerId of the requester. This is only necessary when not using a JWT bearer token.
 * 
 */
export async function scmGetGithub(requester: HttpRequester, gamertag?: string): Promise<HttpResponse<GitHubSignInInfoResponse>> {
  let endpoint = "/api/scm/github";
  
  // Make the API request
  return makeApiRequest<GitHubSignInInfoResponse>({
    r: requester,
    e: endpoint,
    m: GET,
    g: gamertag,
    w: true
  });
}

/**
 * @remarks
 * **Authentication:**
 * This method requires a valid bearer token in the `Authorization` header.
 * 
 * @param requester - The `HttpRequester` type to use for the API request.
 * @param payload - The `StartGitHubSignInRequest` instance to use for the API request
 * @param gamertag - Override the playerId of the requester. This is only necessary when not using a JWT bearer token.
 * 
 */
export async function scmPostGithubSignIn(requester: HttpRequester, payload: StartGitHubSignInRequest, gamertag?: string): Promise<HttpResponse<StartGitHubSignInResponse>> {
  let endpoint = "/api/scm/github/sign-in";
  
  // Make the API request
  return makeApiRequest<StartGitHubSignInResponse, StartGitHubSignInRequest>({
    r: requester,
    e: endpoint,
    m: POST,
    p: payload,
    g: gamertag,
    w: true
  });
}

/**
 * @remarks
 * **Authentication:**
 * This method requires a valid bearer token in the `Authorization` header.
 * 
 * @param requester - The `HttpRequester` type to use for the API request.
 * @param payload - The `CompleteGitHubSignInRequest` instance to use for the API request
 * @param gamertag - Override the playerId of the requester. This is only necessary when not using a JWT bearer token.
 * 
 */
export async function scmPostGithubSignInComplete(requester: HttpRequester, payload: CompleteGitHubSignInRequest, gamertag?: string): Promise<HttpResponse<ScmHostTokenResponse>> {
  let endpoint = "/api/scm/github/sign-in/complete";
  
  // Make the API request
  return makeApiRequest<ScmHostTokenResponse, CompleteGitHubSignInRequest>({
    r: requester,
    e: endpoint,
    m: POST,
    p: payload,
    g: gamertag,
    w: true
  });
}
