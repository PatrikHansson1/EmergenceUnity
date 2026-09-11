import json,glob
res=[]
for f in sorted(glob.glob('a3-*-300.json')):
    d=json.load(open(f)); Y=d['yearly']
    litVY=nonVY=litL=nonL=0; litRe=nonRe=0
    pressLitVY=pressLitL=0; nopressLitVY=nopressLitL=0
    firstW=next((y['y'] for y in Y if y['writing']),None)
    extBefore=sum(len(y['extinct']) for y in Y if firstW is None or y['y']<firstW)
    extAfter=sum(len(y['extinct']) for y in Y if firstW is not None and y['y']>=firstW)
    yrsBefore=(firstW-1) if firstW else len(Y); yrsAfter=len(Y)-yrsBefore
    for y in Y:
        lit={v['name'] for v in y['villages'] if v['literate']}
        for v in y['villages']:
            if v['holds']==0: continue
            if v['literate']:
                litVY+=1
                if y['printpress']: pressLitVY+=1
                else: nopressLitVY+=1
            else: nonVY+=1
        for e in y['lost']:
            if e['v'] in lit:
                litL+=1
                if y['printpress']: pressLitL+=1
                else: nopressLitL+=1
            else: nonL+=1
        for e in y['redisc']:
            if e.get('v') in lit: litRe+=1
            else: nonRe+=1
    Llit=litL/litVY if litVY else None; Lnon=nonL/nonVY if nonVY else None
    R=(Lnon/Llit) if (Llit and Lnon is not None) else (float('inf') if (Llit==0 and litVY>=50 and Lnon) else None)
    measurable=litVY>=50
    p1=measurable and R is not None and R>=2.0
    exb=extBefore/yrsBefore*100 if yrsBefore else None; exa=extAfter/yrsAfter*100 if yrsAfter else None
    p3=(exb is not None and exa is not None and exb>0 and exa<=0.5*exb)
    res.append(dict(seed=d['seed'],firstWriting=firstW,litVY=litVY,nonVY=nonVY,litLost=litL,nonLost=nonL,L_lit=Llit,L_non=Lnon,R=R,measurable=measurable,P1=p1,
        pressLit=(pressLitL/pressLitVY if pressLitVY else None),nopressLit=(nopressLitL/nopressLitVY if nopressLitVY else None),pressVY=pressLitVY,
        extBefore100=exb,extAfter100=exa,P3=p3,litRedisc=litRe,nonRedisc=nonRe))
print(f"{'seed':>15} {'writ':>4} {'litVY':>5} {'nonVY':>5} {'litL':>4} {'nonL':>4} {'L_lit':>7} {'L_non':>7} {'R':>6} {'meas':>4} {'P1':>3} {'press':>7} {'nopress':>7} {'pVY':>4} {'ext<':>6} {'ext>':>6} {'P3':>3}")
for r in res:
    f=lambda x: '-' if x is None else (f'{x:.3f}' if isinstance(x,float) and x!=float('inf') else str(x))
    print(f"{r['seed']:>15} {f(r['firstWriting']):>4} {r['litVY']:>5} {r['nonVY']:>5} {r['litLost']:>4} {r['nonLost']:>4} {f(r['L_lit']):>7} {f(r['L_non']):>7} {f(r['R']):>6} {str(r['measurable']):>4} {str(r['P1']):>3} {f(r['pressLit']):>7} {f(r['nopressLit']):>7} {r['pressVY']:>4} {f(r['extBefore100']):>6} {f(r['extAfter100']):>6} {str(r['P3']):>3}")
meas=[r for r in res if r['measurable']]
print('mätbara världar',len(meas),'/8; P1 uppfyllda',sum(r['P1'] for r in res),'/8 ⇒ A3-P1', 'GRÖN' if sum(r['P1'] for r in res)>=5 else 'RÖD')
print('P3 uppfyllda',sum(r['P3'] for r in res),'/8 (deskriptivt)')
if len(meas)<4: print('EJ MÄTBART (<4/8 världar med ≥50 literate by-år) ⇒ omkörning 600 år enligt fallträdet')
