<script>
  import PageList from './PageList';
  import FeatherIcon from './FeatherIcon';
  import ModalCard from './ModalCard';
  import FilterSearch from './FilterSearch';
  import PaginateArray from './PaginateArray';
  import ComponentButtons from './ComponentButtons';
  import { getServices } from '../services';

  import Uppy from '@uppy/core';
  import AwsS3 from '@uppy/aws-s3';

  export let player;
  export let cloudData;
  const {cloudsaving, players} = getServices();
  const {url} = window.router;

  let searchableCloudData = [];
  let filteredCloudData = [];
  let pagedCloudData = [];

  let isNetworking = false;
  let playerQueryBox;
  let copyFromPlayer;
  let copyError;

  let selectedFile;
  let fileIdToS3OBject = {}
  let fileIdToProgress = {}
  let queuedS3Summaries = []
  const uppy = new Uppy({ debug: window.config.dev, autoProceed: false })

  $: if(cloudData) {
    searchableCloudData = cloudData.manifest.map(data => ({
        ...data,
        simplifiedKey: data.key,
        progress: 0,
        uploading: false,
        uploadingQueued: false,
        fileId: undefined,
        fileName: undefined
      })
    );
  }
  uppy.use(AwsS3, {
    getUploadParameters: async (file) => {
      uploadLog('preparing file for s3 upload', file);
      const s3Summary = fileIdToS3OBject[file.id];
      if (!s3Summary) {
        console.error('there is no s3Summary object on the file. Cannot upload.', file, fileIdToS3OBject);
        return;
      }
      const data = await cloudsaving.fetchUploadUrl(player, [
      {
        newFileSize: file.size,
        s3Object: s3Summary
      }]);
      uploadLog('file data ready', data);
      return {
        method: 'PUT',
        url: data[0].url,
      };

    }
  });
  uppy.on('file-removed', (file) => {
    const s3Summary = fileIdToS3OBject[file.id];
    uploadLog('Removed file', file, s3Summary);

    s3Summary.fileId = undefined;
    s3Summary.progress = 0;
    s3Summary.uploading = false;
    s3Summary.uploadingQueued = false;
    s3Summary.fileName = undefined;

    delete fileIdToS3OBject[file.id];
    delete fileIdToProgress[file.id];
    fileIdToProgress = {...fileIdToProgress}
    pagedCloudData = [...pagedCloudData]
  });
  uppy.on('upload-progress', (file, progress) => {
    uploadLog('PROGRESS', file.id, progress.bytesUploaded, progress.bytesTotal)
    const ratio = progress.bytesUploaded / progress.bytesTotal;
    fileIdToProgress[file.id] = ratio;
    fileIdToProgress = {...fileIdToProgress}
    fileIdToS3OBject[file.id].progress = ratio;
  })
  uppy.on('upload-success', (file, response) => {
    fileIdToProgress[file.id] = 1;
    fileIdToProgress = {...fileIdToProgress}
    fileIdToS3OBject[file.id].progress = 1;
  });
  uppy.on('complete', async (result) => {
    uploadLog('successful files:', result.successful)
    uploadLog('failed files:', result.failed)
    result.successful.forEach(file => {
      uppy.removeFile(file.id)
    });

    result.failed.forEach(file => {
      window.postError('Upload Failed', file.name + ' failed to upload. ' + file.error)
      uppy.removeFile(file.id);
    });
    if (result.failed.length <= 0 ){
      await cloudsaving.moveObjects(player);
    };

  });

  function uploadLog() {
    // XXX: Logs are disabled, but if you need to debug upload process, re-enable here.
    //return;

    if(typeof(console) !== 'undefined') {
      console.log.apply(console, arguments);
    }
  }

  async function getDownloadURL(s3Summary){
    if (s3Summary.uploading) return;
    const objectKey = s3Summary.eTag;
    const url = await cloudsaving.fetchDownloadURLs(player,objectKey);
    downloadURI(url.url,s3Summary.simplifiedKey);
  }

  function downloadURI(uri, name){
    let link = document.createElement("a");
    link.href = uri;
    link.download = name;
    document.body.appendChild(link);
    link.click();
    document.body.removeChild(link);
  }

  function openCopyDialog(toggle){
    resetPlayerCopy();
    toggle();
  }
  function resetPlayerCopy(){
    copyError = undefined;
    copyFromPlayer = undefined;
  }
  async function queryPlayer(){
    try {
      isNetworking = true;
      copyFromPlayer = await players.getPlayer(playerQueryBox.value);
    } catch (e) {
      copyError = 'Unable to find that player.'
    } finally {
      isNetworking = false;
    }

  }
  async function copyData(toggle){
    isNetworking = true;
    try {
      const result = await cloudsaving.copyDataFrom(copyFromPlayer, player)
      toggle();
    } catch (e) {
      copyError = 'Failed to copy data. ' + e.message;
    } finally {
      isNetworking = false;
    }
  }


  function openUpload(s3Summary, toggle){
    if (s3Summary.uploading) return;
    uploadLog('opening for ', s3Summary, s3Summary.fileId);

    // if (s3Summary.fileId){
    //   const fileId = s3Summary.fileId;
    //   var uppyFile = uppy.getFile(fileId);
    //   selectedFile = uppyFile.data;
    //   uppy.removeFile(fileId);
    //   queuedS3Summaries = queuedS3Summaries.filter(summary => summary != s3Summary);
    // } else {
    //   selectedFile = undefined;
    // }

    selectedFile = undefined;


    toggle();
  }

  function removeQueuedUpload(toggle, s3Summary){
    if (s3Summary.fileId){
      const fileId = s3Summary.fileId;
      var uppyFile = uppy.getFile(fileId);
      selectedFile = uppyFile.data;
      uppy.removeFile(fileId);
      queuedS3Summaries = queuedS3Summaries.filter(summary => summary != s3Summary);
      selectedFile = undefined;
    }
  }

  function startQueuedUpload(){
    queuedS3Summaries.forEach(summary => {
      const fileId = summary.fileId;
      summary.uploading = true;
      fileIdToS3OBject[fileId] = summary;
      fileIdToProgress[fileId] = 0;
      summary.uploadingQueued = undefined;

    });
    queuedS3Summaries = [];
    pagedCloudData = [...pagedCloudData]
    uppy.upload();
  }

  function queueUpload(toggle, s3Summary){
    uploadLog('queueing upload', s3Summary);
    if (!selectedFile) {
      throw 'no selected file'
      return;
    }
    try {

      const fileId = uppy.addFile({
        source: 'file input',
        name: selectedFile.name + (new Date().getTime()), // uppy needs distinct file names,
        type: selectedFile.type,
        data: selectedFile
      });
      s3Summary.fileId = fileId;
      s3Summary.fileName = selectedFile.name,
      fileIdToS3OBject[fileId] = s3Summary;
      s3Summary.uploadingQueued = true;
      fileIdToProgress = {...fileIdToProgress}
      pagedCloudData = [...pagedCloudData]
      queuedS3Summaries = [...queuedS3Summaries, s3Summary]

    } catch (err) {
      if (err.isRestriction) {
        console.log('Restriction error:', err)
      } else {
        console.error(err)
      }
    } finally {
      toggle();

    }
  }

  function startImmediateUpload(toggle, s3Summary){
    uploadLog('starting upload', s3Summary);

    if (!selectedFile) {
      throw 'no selected file'
      return;
    }

    try {
      const fileId = uppy.addFile({
        source: 'file input',
        name: selectedFile.name,
        type: selectedFile.type,
        data: selectedFile
      });
      s3Summary.fileId = fileId;
      s3Summary.fileName = selectedFile.name,
      fileIdToS3OBject[fileId] = s3Summary;
      fileIdToProgress[fileId] = 0;
      s3Summary.uploading = true;
      fileIdToProgress = {...fileIdToProgress}
      pagedCloudData = [...pagedCloudData]

      uppy.retryUpload(fileId);

    } catch (err) {
      if (err.isRestriction) {
        console.log('Restriction error:', err)
      } else {
        console.error(err)
      }
    } finally {
      toggle();
    }
  }
  function getFileProgress(s3Summary){
    const fileId = s3Summary.fileId;
    if (!fileId) return -1;

    return fileIdToProgress[fileId];
  }

  function addFileForUpload(event, s3Summary){
    const files = Array.from(event.target.files);
    if (files.length !== 1){
      console.error('Only single file upload is supported.');
      return;
    }
    selectedFile = files[0];
  }
</script>

<div class="cloud-data">

  <ComponentButtons>
    <p class="control">
      <ModalCard class="is-xsmall has-light-bg replace-data">
        <div slot="trigger-evt" let:toggle let:active>
          <button class="button trigger-button" on:click|preventDefault|stopPropagation={evt => openCopyDialog(toggle)}>
              <span class="icon is-small">
                  <FeatherIcon icon="copy"/>
              </span>
              <span>
                  Copy From
              </span>
          </button>
        </div>
        <h3 slot="title">
          Copy From
        </h3>
        <span slot="body">
          {#if copyError}
            <div>
              {copyError}
            </div>

          {:else if !copyFromPlayer}
            <div>
              You can overwrite this user's cloud data with another user's data.
              This process will clear all of the player's cloud data, and replace it with a selected player's data.
              <b> This operation cannot be undone. </b>
            </div>

            <div>
              Please enter the dbid of the player you'd like to copy cloud data from.
            </div>

            <div class="field">
                <div class="control">
                    <input bind:this={playerQueryBox} class="input" type="text" placeholder="[source data's dbid]"/>
                </div>
            </div>
          {:else if copyFromPlayer}
            <div>
              Are you sure you want to replace the cloud data of this user, with the cloud data of
              <a target="_blank" href={url(`/:orgId/games/:gameId/realms/:realmId/players?playerQuery=${copyFromPlayer.gamerTagForRealm()}`)}>
                {copyFromPlayer.gamerTagForRealm()}
              </a>
              <br>
              <b> This operation cannot be undone </b>
            </div>
          {/if}
        </span>

        <span slot="buttons" let:toggle>
          {#if copyError}
            <button class="button is-success" on:click|preventDefault|stopPropagation={evt => resetPlayerCopy()} class:is-loading={isNetworking}>
                <span>
                    <slot name="primary">
                        Back
                    </slot>
                </span>
            </button>
          {:else if copyFromPlayer}
            <button class="button is-success" on:click|preventDefault|stopPropagation={evt => copyData(toggle)} class:is-loading={isNetworking}>
                <span>
                    <slot name="primary-button">
                        Copy Data
                    </slot>
                </span>
            </button>
          {:else}
            <button class="button is-success" on:click|preventDefault|stopPropagation={evt => queryPlayer()} class:is-loading={isNetworking}>
                <span>
                    <slot name="primary-button">
                        Lookup Player
                    </slot>
                </span>
            </button>
          {/if}

          <button class="button cancel" on:click|preventDefault={toggle}>
              <span>Cancel</span>
          </button>
        </span>
      </ModalCard>
    </p>
    {#if queuedS3Summaries.length > 0}
      <p class="control">
        <button class="button trigger-button"
          on:click|preventDefault|stopPropagation={evt => startQueuedUpload()}>
          Upload Queued
        </button>
      </p>
    {/if}
  </ComponentButtons>

  {#if searchableCloudData.length == 0}
    <div class="no-cloud-data-message">
      No cloud data available.
    </div>
  {:else}
    <FilterSearch
            placeholder="Search..."
            allElements={searchableCloudData}
            filterOn="key"
            filterFunctions={[
                    {func: (m) => true, name: 'All'}
            ]}
            bind:value={filteredCloudData}
    >
    </FilterSearch>

    {#if filteredCloudData.length == 0}
      <div class="no-cloud-data-message">
        No cloud data found.
      </div>
    {:else}
      <PaginateArray
              paginationKey="clouddata"
              position="top"
              elements={filteredCloudData}
              bind:pagedElements={pagedCloudData}/>
      <PageList elements={pagedCloudData}>

        <div slot="element" let:element class="elem">
          <div style="flex-grow: 1">
              {element.simplifiedKey}
          </div>
          <div>
            <button class="button trigger-button"
              disabled={element.uploading}
              class:disabled={element.uploading}
              on:click|preventDefault|stopPropagation={evt => getDownloadURL(element)}>
              <span class="icon is-small">
                  <FeatherIcon icon="download-cloud"/>
              </span>
              <span>
                  Download
              </span>
            </button>
          </div>
          <div>
            <ModalCard class="is-xsmall has-light-bg replace-data">
              <div slot="trigger-evt" let:toggle let:active>

                  <button class="button trigger-button uploader-trigger-button leftish"
                    class:is-loading={element.uploading}
                    on:click|preventDefault|stopPropagation={evt => openUpload(element, toggle)}>

                      {#if element.uploadingQueued}
                        <span class="existing-file">
                          <!-- {uppy.getFile(element.fileId).name} -->
                          {element.fileName}
                        </span>
                      {:else}
                        <span class="icon is-small">
                            <FeatherIcon icon="upload-cloud"/>
                        </span>
                        <span>
                          Upload
                        </span>
                      {/if}

                      {#if fileIdToProgress[element.fileId] != undefined}
                        <span class="progress-number">
                          {Math.floor(fileIdToProgress[element.fileId]*100)} %
                        </span>
                        <progress class="progress is-info" value="{fileIdToProgress[element.fileId]}" max="1"></progress>
                      {/if}
                  </button>
              </div>
              <h3 slot="title">
                Upload
              </h3>
              <span slot="body" let:active>
                {#if active}
                  <div class="uploader-container">

                    {#if element.uploadingQueued}
                      <div style="display: flex; flex-direction: column; flex-grow: 1">
                        <b> Queued file </b>
                        <div> {element.fileName} </div>
                      </div>
                    {:else}
                      <input type="file" id="file" on:change={evt => addFileForUpload(evt, element)}/>
                      <label for="file" class:file-selected={selectedFile}>
                          <div class="icon" style="width: 60px;">
                            <FeatherIcon icon="upload-cloud" width="60px" height="60px"/>
                          </div>
                          {#if selectedFile}
                            <div class="uploader-title"> {selectedFile.name} </div>
                          {:else}
                            <div class="uploader-title"> Select File </div>
                          {/if}
                      </label>
                    {/if}

                  </div>
                {/if}
              </span>

              <span slot="buttons" let:toggle>
                <button class="button cancel" on:click|preventDefault={toggle}>
                    <span>Cancel</span>
                </button>

                {#if element.uploadingQueued}
                  <button class="button is-info"
                    class:disabled={selectedFile}
                    on:click|preventDefault|stopPropagation={evt => removeQueuedUpload(toggle, element)}
                    disabled={selectedFile}>
                      <span>
                          <slot name="primary-button">
                              Dequeue
                          </slot>
                      </span>
                  </button>
                {:else}
                  <button class="button is-info"
                    class:disabled={!selectedFile}
                    on:click|preventDefault|stopPropagation={evt => startImmediateUpload(toggle, element)}
                    disabled={!selectedFile}>
                      <span>
                          <slot name="primary-button">
                              Upload
                          </slot>
                      </span>
                  </button>

                  <button class="button is-info"
                    class:disabled={!selectedFile}
                    on:click|preventDefault|stopPropagation={evt => queueUpload(toggle, element)}
                    disabled={!selectedFile}>
                      <span>
                          <slot name="primary-button">
                              Queue
                          </slot>
                      </span>
                  </button>
                {/if}

              </span>
            </ModalCard>
          </div>
        </div>
      </PageList>
      <PaginateArray
              paginationKey="clouddata"
              position="bottom"
              elements={filteredCloudData}
              bind:pagedElements={pagedCloudData}/>
    {/if}
  {/if}
</div>

<style lang="scss">
  $red: #FF5c5c;
  $green: #77B769;
  $blue: #4497B7;
  $yellow: #EBC65F;
  $upload-color: #0095f1;

  .cloud-data {
    position: relative;
  }

  .uploader-trigger-button {
    position: relative;

    max-width: 86px;
    .existing-file {
      text-overflow: ellipsis;
      white-space: nowrap;
      overflow: hidden;
      line-height: 1;
    }
    .progress-number {
      color: darkslategray;
      position: absolute;
    }
    progress {
      position: absolute;
      bottom: -1px;
      left: -1px;
      right: -1px;
      height: 5px;
    }
  }
  .button.is-loading.leftish:after {
    left: 6px;
  }

  .uploader-container {
    display:flex;
    padding-bottom: 12px;
    .uploader-title {
      color: #e6f3ff;
    }
  }

  [type="file"] {
    border: 0;
    clip: rect(0, 0, 0, 0);
    height: 1px;
    overflow: hidden;
    padding: 0;
    position: absolute !important;
    white-space: nowrap;
    width: 1px;
  }

  [type="file"] + label {

    cursor: pointer;
    display: inline-block;
    flex-grow: 1;
    border-radius: 3px;
    color: white;
    border: dashed 4px $upload-color;
    .icon {
      color: $upload-color;
      padding-top:30px;
    }
    background-color: transparent;
    padding: 6px 0px;
  }

  [type="file"]:focus + label,
  [type="file"] + label:hover {
      background-color: rgba(0,0,0,.1);
      border-color: lighten($upload-color, 10%);
      .icon {
        color: lighten($upload-color, 10%);
      }
  }

  [type="file"]:focus + label {
    outline: 1px dotted #000;
  }

  pre {
    color: white;
    background: black;
    text-align: left;
    user-select: text;
    cursor: text;
    max-height: 500px;
  }

  .trigger-button {
    padding: 6px 12px;
  }
  p.modal-wrap {
    padding-top: 5px;
  }
</style>
