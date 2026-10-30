import concurrent.futures,http.client,json,os,pathlib,socket,subprocess,time
root=pathlib.Path(__file__).resolve().parent
dll=root/'bin/Release/net10.0/BeamStopProbeServer.dll'
def wait_file(path,proc,timeout=5):
 end=time.monotonic()+timeout
 while time.monotonic()<end:
  if path.exists(): return json.loads(path.read_text())
  if proc.poll() is not None: raise AssertionError(f'Server exited {proc.returncode}: {path}')
  time.sleep(.01)
 raise TimeoutError(str(path))
def case(name,cancel_first,observe=False):
 control=root/('smoke-'+name+'-'+str(time.time_ns()));control.mkdir()
 with socket.socket() as reservation:
  reservation.bind(('127.0.0.1',0));port=reservation.getsockname()[1]
 env=os.environ.copy();env['PROBE_RETRY_STOP']='1';env['PROBE_STOP_STATUS']='500' if not cancel_first else '204'
 if observe:(control/'config.json').write_text(json.dumps({'awaitSignalObservation':True}))
 log=(control/'server.log').open('w')
 command=([env['PROBE_SERVER_EXECUTABLE']] if env.get('PROBE_SERVER_EXECUTABLE') else ['dotnet',str(dll)]) + [str(port),str(control)]
 proc=subprocess.Popen(command,env=env,stdout=log,stderr=subprocess.STDOUT,creationflags=getattr(subprocess,'CREATE_NO_WINDOW',0))
 client=http.client.HTTPConnection('127.0.0.1',port,timeout=5)
 body=json.dumps({'requestId':'independent-smoke','commandLine':'beam content ps --watch'})
 def post_stop(timeout):
  c=http.client.HTTPConnection('127.0.0.1',port,timeout=timeout)
  try:
   c.request('POST','/stop',body,{'Content-Type':'application/json'})
   response=c.getresponse(); response.read();return response.status
  except TimeoutError:return 'timeout'
  finally:c.close()
 try:
  wait_file(control/'ready.json',proc)
  (control/'allow-headers').touch();(control/'release-worker').touch()
  client.request('POST','/execute',body,{'Content-Type':'application/json'})
  if observe:
   wait_file(control/'execute-received.json',proc)
   time.sleep(.1)
   assert not (control/'headers-sent.json').exists()
   (control/'observed-execute').touch()
  response=client.getresponse()
  assert response.getheader('X-Beam-Request-Id')=='independent-smoke'
  assert response.readline()==b': ready\n'
  with concurrent.futures.ThreadPoolExecutor() as pool:
   first=pool.submit(post_stop,.4 if cancel_first else 5)
   received=wait_file(control/'stop-received.json',proc)
   assert received['data']['requestId']=='independent-smoke'
   if observe:
    time.sleep(.1)
    assert not (control/'execute-closed.json').exists()
    (control/'observed-stop').touch()
   assert response.read()==b'data: {"type":"test","data":{},"ts":1}\n'
   assert not (control/'ack-sent.json').exists()
   if cancel_first:assert first.result(timeout=2)=='timeout'
   else:assert not first.done()
   (control/'allow-ack').touch()
   wait_file(control/'ack-sent.json',proc)
   if not cancel_first:assert first.result(timeout=2)==500
   if observe:(control/'allow-retry-ack').touch()
   retry=pool.submit(post_stop,5)
   wait_file(control/'retry-received.json',proc)
   assert not retry.done()
   if observe:
    time.sleep(.1)
    assert not retry.done()
    assert not (control/'retry-ack-sent.json').exists()
    (control/'observed-retry').touch()
   (control/'allow-retry-ack').touch()
   assert retry.result(timeout=2)==204
  assert proc.wait(timeout=5)==0
  print(name+': PASS (independent HTTP client verified header, SSE, held ack, and retry; logs '+str(control)+')')
 finally:
  client.close()
  if proc.poll() is None:
   (control/'shutdown').touch()
   try:proc.wait(timeout=3)
   except subprocess.TimeoutExpired:proc.kill();proc.wait()
  log.close()
case('failed-status-retry',False)
case('cancelled-client-retry',True)
case('signal-observation-gates',False,True)

def case_info_configuration():
 control=root/('smoke-info-config-'+str(time.time_ns()));control.mkdir()
 with socket.socket() as reservation:
  reservation.bind(('127.0.0.1',0));port=reservation.getsockname()[1]
 env=os.environ.copy()
 (control/'config.json').write_text(json.dumps({'infoPids':[1],'owner':'stale','version':'stale'}))
 command=([env['PROBE_SERVER_EXECUTABLE']] if env.get('PROBE_SERVER_EXECUTABLE') else ['dotnet',str(dll)]) + [str(port),str(control)]
 with (control/'server.log').open('w') as log:
  proc=subprocess.Popen(command,env=env,stdout=log,stderr=subprocess.STDOUT,creationflags=getattr(subprocess,'CREATE_NO_WINDOW',0))
  try:
   wait_file(control/'ready.json',proc)
   (control/'config.tmp').write_text(json.dumps({'infoPids':[987,654],'owner':'independent-owner','version':'0.0.123','retryStop':False,'stopStatus':204,'writeOutput':True}))
   os.replace(control/'config.tmp',control/'config.json')
   for expected in [987,654]:
    c=http.client.HTTPConnection('127.0.0.1',port,timeout=3)
    try:
     c.request('GET','/info')
     result=json.loads(c.getresponse().read())
     assert result=={'owner':'independent-owner','version':'0.0.123','pid':expected},result
    finally:c.close()
   assert proc.wait(timeout=5)==0
   print('info-config-after-ready: PASS (two discovery replies used final settings; logs '+str(control)+')')
  finally:
   if proc.poll() is None:
    (control/'shutdown').touch()
    try:proc.wait(timeout=3)
    except subprocess.TimeoutExpired:proc.kill();proc.wait()
case_info_configuration()
