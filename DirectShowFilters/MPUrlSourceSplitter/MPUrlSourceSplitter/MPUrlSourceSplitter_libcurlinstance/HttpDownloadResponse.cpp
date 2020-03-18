/*
    Copyright (C) 2007-2010 Team MediaPortal
    http://www.team-mediaportal.com

    This file is part of MediaPortal 2

    MediaPortal 2 is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    MediaPortal 2 is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with MediaPortal 2.  If not, see <http://www.gnu.org/licenses/>.
*/

#include "StdAfx.h"

#include "HttpDownloadResponse.h"

#include "CurlInstance.h"

CHttpDownloadResponse::CHttpDownloadResponse(HRESULT *result)
  : CDownloadResponse(result)
{
  this->headers = NULL;
  this->responseCode = 0;
  this->lastUsedUrl = NULL;

  if ((result != NULL) && (SUCCEEDED(*result)))
  {
    this->headers = new CHttpHeaderCollection(result);
    CHECK_POINTER_HRESULT(*result, this->headers, *result, E_OUTOFMEMORY);
  }
}

CHttpDownloadResponse::~CHttpDownloadResponse(void)
{
  FREE_MEM_CLASS(this->headers);
  FREE_MEM(this->lastUsedUrl);
}

/* get methods */

CHttpHeaderCollection *CHttpDownloadResponse::GetHeaders(void)
{
  return this->headers;
}

bool CHttpDownloadResponse::GetRangesSupported(void)
{
  return this->IsSetFlags(HTTP_DOWNLOAD_RESPONSE_FLAG_RANGES_SUPPORTED);
}

long CHttpDownloadResponse::GetResponseCode(void)
{
  return this->responseCode;
}

const wchar_t *CHttpDownloadResponse::GetLastUsedUrl(void)
{
  return this->lastUsedUrl;
}

HRESULT CHttpDownloadResponse::GetResultError(void)
{
  // result error is sometimes set to S_OK, even if HTTP error occured

  // check result error and set if necessary
  if ((__super::GetResultError() == S_OK) && IS_RESPONSE_CODE_ERROR(this->GetResponseCode()))
  {
    this->SetResultError(HRESULT_FROM_CURL_CODE(CURLE_HTTP_RETURNED_ERROR));
  }

  return __super::GetResultError();
}

/* set methods */

void CHttpDownloadResponse::SetRangesSupported(bool rangesSupported)
{
  this->flags &= ~HTTP_DOWNLOAD_RESPONSE_FLAG_RANGES_SUPPORTED;
  this->flags = (rangesSupported) ? HTTP_DOWNLOAD_RESPONSE_FLAG_RANGES_SUPPORTED : HTTP_DOWNLOAD_RESPONSE_FLAG_NONE;
}

void CHttpDownloadResponse::SetResponseCode(long responseCode)
{
  this->responseCode = responseCode;
}

bool CHttpDownloadResponse::SetLastUsedUrl(const wchar_t *lastUsedUrl)
{
  SET_STRING_RETURN_WITH_NULL(this->lastUsedUrl, lastUsedUrl);
}

/* other methods */

/* protected methods */

CDownloadResponse *CHttpDownloadResponse::CreateDownloadResponse(void)
{
  HRESULT result = S_OK;
  CHttpDownloadResponse *response = new CHttpDownloadResponse(&result);
  CHECK_POINTER_HRESULT(result, response, result, E_OUTOFMEMORY);

  CHECK_CONDITION_EXECUTE(FAILED(result), FREE_MEM_CLASS(response));
  return response;
}

bool CHttpDownloadResponse::CloneInternal(CDownloadResponse *clone)
{
  bool result = __super::CloneInternal(clone);

  if (result)
  {
    CHttpDownloadResponse *response = dynamic_cast<CHttpDownloadResponse *>(clone);

    response->responseCode = this->responseCode;
    response->headers->Clear();
    result &= response->headers->Append(this->headers);
  }
  return result;
}